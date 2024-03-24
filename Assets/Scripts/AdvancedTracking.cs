using MediaPipe.HandPose;
using UnityEngine;
using JointType = Windows.Kinect.JointType;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System;

namespace UserOnboarding
{
    // Combines the captation from kinect and from HandPoseBarracuda
    // it provides body points, with both left and right hand, with each articulations
    public class BodyPointsFromKinectAugmented : BodyPointsProvider
    {
        [SerializeField] InputMode[] inputModes = { InputMode.Color };
        [SerializeField] KinectHandle kinectHandle;
        [SerializeField] ResourceSet resourceSet;
        [SerializeField] ComputeShader minMaxShader;
        [SerializeField] ComputeShader dynamicContrastShader;
        [SerializeField] ComputeShader colorizeShader;
        [SerializeField] BodySelection bodySelection = BodySelection.AnyTracked;
        public enum BodySelection
        {
            AnyTracked,
            AnyTrackedLock,
            IAmVulcan,
            AtIndex0,
            AtIndex1,
            AtIndex2,
            AtIndex3,
            AtIndex4,
            AtIndex5,
            AtIndex6,
            AtIndex7,
        }
        const int KERNEL_SIZE = 512;
        private int lockedBody = -1;
        private KinectHandle.Body body = KinectHandle.Body.NotProvided;

        // which video feed from kinect to send to HandPoseBarracuda
        public enum InputMode
        {
            Color,
            Infrared,
            // the combine is an experiment to produce an a image by combining both color and infrared
            // but it doesn't work well as it needs a way to calibrate Kinect's camera position for a correct superposition
            Combined,
        }

        // when cropping image on hand center, how much distance (m) from center to side of cropping
        private const float BOUNDING_BOX_RADIUS = 0.18f;
        private class Hand
        {
            // holds the different compute shaders and buffer for HandPoseBarracuda hand detection
            public HandTracking[] pipelines;
            public (float min, float max) handedness;
            public Vector3 pos;
            // which kinect joint to query for hand center
            public JointType center;
            // which kinect joint to query for hand wrist
            public JointType wrist;
            public RenderTexture[] textures;
            // where we store data outputed by HandPoseBarracuda
            public (PointState state, Vector3 pos)[] points;
            // the score is used to choose weither to update or not the positions:
            // - the score is reducing by itself over time
            // - we only update if the new positions have a better score
            public float score;
        }
        private Hand[] Hands => new[] { hand.left, hand.right };
        private (Hand left, Hand right) hand;
        private RenderTexture minMax1;
        private RenderTexture minMax2;

        // infrared video feed from Kinect
        private KinectHandle.Source ir;
        // color video feed from Kinect
        private KinectHandle.Source cl;

        void Start()
        {
            Assert.IsNotNull(kinectHandle);
            Assert.IsNotNull(resourceSet);
            Assert.IsNotNull(minMaxShader);
            Assert.IsNotNull(dynamicContrastShader);
            Assert.IsNotNull(colorizeShader);
            Assert.IsTrue(inputModes.Length > 0);

            ir = null;
            cl = null;
            minMax1 = new RenderTexture(256, 256, 0) { enableRandomWrite = true };
            minMax2 = new RenderTexture(256, 256, 0) { enableRandomWrite = true };
            hand.left = new()
            {
                pipelines = inputModes.Select(_ => new HandTracking(resourceSet)).ToArray(),
                handedness = (min: 0.8f, max: 1.2f),// HandPoseBarracuda gives handedness as a value around 1f for left hand
                textures = inputModes.Select(_ => new RenderTexture(KERNEL_SIZE, KERNEL_SIZE, 0) { enableRandomWrite = true }).ToArray(),
                pos = Vector3.zero,
                center = JointType.HandLeft,
                wrist = JointType.WristLeft,
                points = Enumerable.Repeat((PointState.NotProvided, Vector3.zero), HandTracking.KeyPointCount).ToArray(),
                score = 0.0f,
            };
            hand.right = new()
            {
                pipelines = inputModes.Select(_ => new HandTracking(resourceSet)).ToArray(),
                handedness = (min: -0.2f, max: 0.2f),// HandPoseBarracuda gives handedness as a value around 0f for right hand
                textures = inputModes.Select(_ => new RenderTexture(KERNEL_SIZE, KERNEL_SIZE, 0) { enableRandomWrite = true }).ToArray(),
                pos = Vector3.zero,
                center = JointType.HandRight,
                wrist = JointType.WristRight,
                points = Enumerable.Repeat((PointState.NotProvided, Vector3.zero), HandTracking.KeyPointCount).ToArray(),
                score = 0.0f,
            };

            // we only open the color or infrared video feeds if we use them
            // this is depending on the desired input mode
            var useCl = false;
            var useIr = false;
            foreach (var inputMode in inputModes)
            {
                switch (inputMode)
                {
                    case InputMode.Color:
                        useCl = true;
                        break;
                    case InputMode.Infrared:
                        useIr = true;
                        break;
                    case InputMode.Combined:
                        useCl = true;
                        useIr = true;
                        break;
                }
            }
            if (useCl && useIr)
            {
                cl = kinectHandle.Cl;
                ir = kinectHandle.Ir;
                ir.Changed += HandAnalysis;
            }
            else if (useCl)
            {
                cl = kinectHandle.Cl;
                cl.Changed += HandAnalysis;
            }
            else if (useIr)
            {
                ir = kinectHandle.Ir;
                ir.Changed += HandAnalysis;
            }

            kinectHandle.BodyPointsChanged += OnKinectBodiesChange;

            // allows inspection of internal render textures
            Transform go;
            for (int i = 0; i < inputModes.Length; i++)
            {
                // if a child is called in a specific way, and has a MeshRenderer
                go = transform.Find($"Inspect {i} Left");
                if (go != null)
                {
                    go.GetComponent<MeshRenderer>().material.mainTexture = hand.left.textures[i];
                }
                go = transform.Find($"Inspect {i} Right");
                if (go != null)
                {
                    go.GetComponent<MeshRenderer>().material.mainTexture = hand.right.textures[i];
                }
            }
        }

        int SelectBody()
        {
            switch (bodySelection)
            {
                case BodySelection.AnyTracked:
                    if (kinectHandle.TrackedBodies.Length == 0) return -1;
                    return kinectHandle.TrackedBodies[0];
                case BodySelection.AnyTrackedLock:
                    if (lockedBody >= 0) return lockedBody;
                    if (kinectHandle.TrackedBodies.Length == 0) return -1;
                    lockedBody = kinectHandle.TrackedBodies[0];
                    return lockedBody;
                case BodySelection.AtIndex0:
                    return 0;
                case BodySelection.AtIndex1:
                    return 1;
                case BodySelection.AtIndex2:
                    return 2;
                case BodySelection.AtIndex3:
                    return 3;
                case BodySelection.AtIndex4:
                    return 4;
                case BodySelection.AtIndex5:
                    return 5;
                case BodySelection.AtIndex6:
                    return 6;
                case BodySelection.AtIndex7:
                    return 7;
            }
            return -1;
        }

        private static PointState PointStateFromScore(float score)
        {
            if (score > 0.8f) return PointState.Tracked;
            if (score > 0.7f) return PointState.Inferred;
            return PointState.NotTracked;
        }
        void OnKinectBodiesChange()
        {
            var selectedBody = SelectBody();
            if (selectedBody == -1) return;
            body = kinectHandle.GetBody(selectedBody);

            foreach (var hand in Hands)
            {
                var center = body.Get(hand.center);
                var wrist = body.Get(hand.wrist);
                if (center.state == PointState.Tracked && wrist.state == PointState.Tracked)
                {
                    // when Kinect gives a new hand position
                    hand.pos = center.pos;
                    var dif = wrist.pos - hand.points[0].pos;
                    // we update all non-kinect point to move accordingly to Kinect info
                    for (int i = 0; i < HandTracking.KeyPointCount; i++)
                    {
                        hand.points[i].state = PointStateFromScore(hand.score);
                        hand.points[i].pos += dif;
                    }
                }
            }
            RaiseBodyPointsChanged();
        }

        void DynamicConstrast(Texture texture)
        {
            Assert.IsTrue(texture.width == KERNEL_SIZE);
            Assert.IsTrue(texture.height == KERNEL_SIZE);

            var readFrom = texture;
            var swap = false;
            for (int i = KERNEL_SIZE / 2; i > 0; i /= 2, swap = !swap)
            {
                var size = Math.Max(i / 8, 1);
                var writeTo = swap ? minMax1 : minMax2;
                minMaxShader.SetTexture(0, "input", readFrom);
                minMaxShader.SetTexture(0, "output", writeTo);
                minMaxShader.Dispatch(0, size, size, 1);
                readFrom = writeTo;
            }

            // apply dynamic contrast by dividing by the max value
            dynamicContrastShader.SetTexture(0, "result", texture);
            dynamicContrastShader.SetTexture(0, "max", readFrom);
            dynamicContrastShader.Dispatch(0, KERNEL_SIZE / 8, KERNEL_SIZE / 8, 1);
        }

        // when receiving new frame from video feed, launch HandPoseBarracuda analysis on it
        void HandAnalysis()
        {
            // reduce hand captation score over time, meaning, old data as lower score
            hand.left.score = Mathf.Max(hand.left.score - 0.05f, 0.7f);
            hand.right.score = Mathf.Max(hand.right.score - 0.05f, 0.7f);

            // if the previous computation is still ongoing, just don't start a new
            foreach (var hand in Hands)
            {
                foreach (var pipeline in hand.pipelines)
                {
                    if (pipeline.Busy) return;
                }
            }

            // one analysis is ran per hands and per input method
            // the effect of having several input methods in parallel, is a competitive captation
            // were we only take the one giving the better score
            foreach (var (hand, index) in Hands.Select((v, i) => (v, i)))
            {
                for (int i = 0; i < inputModes.Length; i++)
                {
                    var mode = inputModes[i];
                    var pipeline = hand.pipelines[i];
                    // first, we are going to isolate the hand in a square texture
                    var texture = hand.textures[i];
                    switch (mode)
                    {
                        case InputMode.Color:
                            cl.Crop(hand.pos, BOUNDING_BOX_RADIUS, texture);
                            break;
                        case InputMode.Infrared:
                            ir.Crop(hand.pos, BOUNDING_BOX_RADIUS, texture);
                            // on infrared, we have to apply a dynamic contrast filter
                            // it makes sure, the maximum value is always 1.0
                            DynamicConstrast(texture);
                            break;
                        case InputMode.Combined:
                            ir.Crop(hand.pos, BOUNDING_BOX_RADIUS, texture);
                            DynamicConstrast(texture);
                            // on combined, we augment the infrared with color information from color video feed
                            colorizeShader.SetTexture(0, "color", cl.texture);
                            colorizeShader.SetVector("box", cl.BoundingBox(hand.pos, BOUNDING_BOX_RADIUS));
                            colorizeShader.SetTexture(0, "result", texture);
                            colorizeShader.Dispatch(0, KERNEL_SIZE / 8, KERNEL_SIZE / 8, 1);
                            break;
                    }
                    // now the HandPoseBarracude analysis is launched
                    pipeline.ProcessImage(texture);
                    // when and only when the result will be available, then...
                    pipeline.BodyPointsUpdatedEvent += () =>
                    {
                        // only update if score is better then previous and handedness is correct
                        if (pipeline.Score <= hand.score) return;
                        var h = pipeline.Handedness;
                        if (h > hand.handedness.min && h < hand.handedness.max)
                        {
                            hand.score = pipeline.Score;
                            // to properly scale the hand, we naively assume the distance
                            // from wrist to index base should be around 10cm
                            var ref1 = pipeline.GetWrist;
                            var ref2 = pipeline.GetIndex1;
                            var dist = Vector3.Distance(ref1, ref2);
                            if (dist > 0.1f && dist < 4f)
                            {
                                var scale = 0.1f / dist;

                                var wristAbs = body.Get(hand.wrist).pos;
                                var wristRel = pipeline.GetWrist;
                                for (int i = 0; i < HandTracking.KeyPointCount; i++)
                                {
                                    // each point is positioned in Kinect space
                                    var posRel = (Vector3)pipeline.HandPoints[i];
                                    hand.points[i] = (
                                        PointStateFromScore(hand.score),
                                        (posRel - wristRel) * scale + wristAbs
                                    );
                                }
                                RaiseBodyPointsChanged();
                            }
                        }
                    };
                }
            }
        }

        void OnDestroy()
        {
            kinectHandle.BodyPointsChanged -= OnKinectBodiesChange;
            if (cl != null)
            {
                cl.Changed -= HandAnalysis;
            }
            if (ir != null)
            {
                ir.Changed -= HandAnalysis;
            }
            foreach (var hand in Hands)
            {
                foreach (var pipeline in hand.pipelines)
                {
                    pipeline.Dispose();
                }
            }
        }

        // because, this class combines different body points detectors,
        // we need to pipe the request depending on what point is requested
        public override (PointState, Vector3) GetBodyPoint(BodyPoint key)
        {
            if (!providedPoints.ContainsKey(key))
            {
                return (PointState.NotProvided, Vector3.zero);
            }
            var (origin, index) = providedPoints[key];
            return origin switch
            {
                PointOrigin.HandLeft => hand.left.points[index],
                PointOrigin.HandRight => hand.right.points[index],
                PointOrigin.Body => body.Get(passThrough[index]),
                _ => throw new InvalidOperationException()
            };
        }


        public override BodyPoint[] ProvidedPoints => providedPoints.Keys.ToArray();


        // the first int:
        // 0 = point is on left hand
        // 1 = point is on right hand
        // 2 = point is rest of body, handled by kinect
        // it would be much better to use algebraic data type instead of a (int, int), but saddly, not available
        // LeftHand(HandPoint)
        // RightHand(HandPoint)
        // Kinect()
        enum PointOrigin { HandLeft, HandRight, Body };
        Dictionary<BodyPoint, (PointOrigin from, int index)> providedPoints = new()
        {
            [BodyPoint.LeftWrist] = (PointOrigin.HandLeft, 0),
            [BodyPoint.LeftThumb1] = (PointOrigin.HandLeft, 1),
            [BodyPoint.LeftThumb2] = (PointOrigin.HandLeft, 2),
            [BodyPoint.LeftThumb3] = (PointOrigin.HandLeft, 3),
            [BodyPoint.LeftThumb] = (PointOrigin.HandLeft, 4),
            [BodyPoint.LeftIndex1] = (PointOrigin.HandLeft, 5),
            [BodyPoint.LeftIndex2] = (PointOrigin.HandLeft, 6),
            [BodyPoint.LeftIndex3] = (PointOrigin.HandLeft, 7),
            [BodyPoint.LeftIndex] = (PointOrigin.HandLeft, 8),
            [BodyPoint.LeftMiddle1] = (PointOrigin.HandLeft, 9),
            [BodyPoint.LeftMiddle2] = (PointOrigin.HandLeft, 10),
            [BodyPoint.LeftMiddle3] = (PointOrigin.HandLeft, 11),
            [BodyPoint.LeftMiddle] = (PointOrigin.HandLeft, 12),
            [BodyPoint.LeftRing1] = (PointOrigin.HandLeft, 13),
            [BodyPoint.LeftRing2] = (PointOrigin.HandLeft, 14),
            [BodyPoint.LeftRing3] = (PointOrigin.HandLeft, 15),
            [BodyPoint.LeftRing] = (PointOrigin.HandLeft, 16),
            [BodyPoint.LeftPinky1] = (PointOrigin.HandLeft, 17),
            [BodyPoint.LeftPinky2] = (PointOrigin.HandLeft, 18),
            [BodyPoint.LeftPinky3] = (PointOrigin.HandLeft, 19),
            [BodyPoint.LeftPinky] = (PointOrigin.HandLeft, 20),

            [BodyPoint.RightWrist] = (PointOrigin.HandRight, 0),
            [BodyPoint.RightThumb1] = (PointOrigin.HandRight, 1),
            [BodyPoint.RightThumb2] = (PointOrigin.HandRight, 2),
            [BodyPoint.RightThumb3] = (PointOrigin.HandRight, 3),
            [BodyPoint.RightThumb] = (PointOrigin.HandRight, 4),
            [BodyPoint.RightIndex1] = (PointOrigin.HandRight, 5),
            [BodyPoint.RightIndex2] = (PointOrigin.HandRight, 6),
            [BodyPoint.RightIndex3] = (PointOrigin.HandRight, 7),
            [BodyPoint.RightIndex] = (PointOrigin.HandRight, 8),
            [BodyPoint.RightMiddle1] = (PointOrigin.HandRight, 9),
            [BodyPoint.RightMiddle2] = (PointOrigin.HandRight, 10),
            [BodyPoint.RightMiddle3] = (PointOrigin.HandRight, 11),
            [BodyPoint.RightMiddle] = (PointOrigin.HandRight, 12),
            [BodyPoint.RightRing1] = (PointOrigin.HandRight, 13),
            [BodyPoint.RightRing2] = (PointOrigin.HandRight, 14),
            [BodyPoint.RightRing3] = (PointOrigin.HandRight, 15),
            [BodyPoint.RightRing] = (PointOrigin.HandRight, 16),
            [BodyPoint.RightPinky1] = (PointOrigin.HandRight, 17),
            [BodyPoint.RightPinky2] = (PointOrigin.HandRight, 18),
            [BodyPoint.RightPinky3] = (PointOrigin.HandRight, 19),
            [BodyPoint.RightPinky] = (PointOrigin.HandRight, 20),

            [BodyPoint.Head] = (PointOrigin.Body, 0),
            [BodyPoint.Neck] = (PointOrigin.Body, 1),
            [BodyPoint.SpineShoulder] = (PointOrigin.Body, 2),
            [BodyPoint.LeftShoulder] = (PointOrigin.Body, 3),
            [BodyPoint.RightShoulder] = (PointOrigin.Body, 4),
            [BodyPoint.LeftElbow] = (PointOrigin.Body, 5),
            [BodyPoint.RightElbow] = (PointOrigin.Body, 6),
        };
        private static readonly JointType[] passThrough = new[]
        {
            JointType.Head,
            JointType.Neck,
            JointType.SpineShoulder,
            JointType.ShoulderLeft,
            JointType.ShoulderRight,
            JointType.ElbowLeft,
            JointType.ElbowRight,
        };
    }
}
