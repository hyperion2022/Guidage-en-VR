using MediaPipe.HandPose;
using UnityEngine;
using JointType = Windows.Kinect.JointType;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Assertions;

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
    public enum BodySelection {
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
    private int lockedBody = -1;
    private KinectHandle.Body body = null;

    // which video feed from kinect to send to HandPoseBarracuda
    public enum InputMode
    {
        Color,
        Infrared,
        // the combine is an experiment to produce an a image by combining both color and infrared
        // but it doesn't work well as it needs a way to calibrate Kinect's camera position for a correct superposition
        Combined,
    }

    private class Hand
    {
        // which hand, for humans from planet earth, 0 for left hand, 1 for right
        // sorry, we don't support non-binary hands, but we strive to be more inclusive in the future
        public int index;
        // holds the different compute shaders and buffer for HandPoseBarracuda hand detection
        public HandTracking[] pipelines;
        public Vector3 pos;
        // which kinect joint to query for hand center
        public JointType center;
        // which kinect joint to query for hand wrist
        public JointType wrist;
        public RenderTexture[] textures;
        // where we store data outputed by HandPoseBarracuda
        public Vector4[] points;
        // the score is used to choose weither to update or not the positions:
        // - the score is reducing by itself over time
        // - we only update if the new positions have a better score
        public float score;
    }
    private Hand[] hands;
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
        hands = new Hand[]{
            new(){
                index = 0,
                pipelines = inputModes.Select(_ => new HandTracking(resourceSet)).ToArray(),
                textures = inputModes.Select(_ => new RenderTexture(512, 512, 0){enableRandomWrite = true}).ToArray(),
                pos = Vector3.zero,
                center = JointType.HandLeft,
                wrist = JointType.WristLeft,
                points = Enumerable.Repeat(absent, 21).ToArray(),
                score = 0.7f,
            },
            new(){
                index = 1,
                pipelines = inputModes.Select(_ => new HandTracking(resourceSet)).ToArray(),
                textures = inputModes.Select(_ => new RenderTexture(512, 512, 0){enableRandomWrite = true}).ToArray(),
                pos = Vector3.zero,
                center = JointType.HandRight,
                wrist = JointType.WristRight,
                points = Enumerable.Repeat(absent, 21).ToArray(),
                score = 0.7f,
            },
        };
        kinectHandle.OpenBody();

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

        kinectHandle.BodiesChanged += OnKinectBodiesChange;

        // allows inspection of internal render textures
        Transform go;
        for (int i = 0; i < inputModes.Length; i++)
        {
            // if a child is called in a specific way, and has a MeshRenderer
            go = transform.Find($"Inspect {i} Left");
            if (go != null)
            {
                go.GetComponent<MeshRenderer>().material.mainTexture = hands[0].textures[i];
            }
            go = transform.Find($"Inspect {i} Right");
            if (go != null)
            {
                go.GetComponent<MeshRenderer>().material.mainTexture = hands[1].textures[i];
            }
        }
    }

    KinectHandle.Body SelectBody() {
        switch (bodySelection) {
            case BodySelection.AnyTracked:
                if (kinectHandle.TrackedBodies.Length == 0) return null;
                return kinectHandle.GetBody(kinectHandle.TrackedBodies[0]);
            case BodySelection.AnyTrackedLock:
                if (lockedBody >= 0) kinectHandle.GetBody(lockedBody);
                if (kinectHandle.TrackedBodies.Length == 0) return null;
                lockedBody = kinectHandle.TrackedBodies[0];
                return kinectHandle.GetBody(lockedBody);
            case BodySelection.AtIndex0:
                return kinectHandle.GetBody(0);
            case BodySelection.AtIndex1:
                return kinectHandle.GetBody(1);
            case BodySelection.AtIndex2:
                return kinectHandle.GetBody(2);
            case BodySelection.AtIndex3:
                return kinectHandle.GetBody(3);
            case BodySelection.AtIndex4:
                return kinectHandle.GetBody(4);
            case BodySelection.AtIndex5:
                return kinectHandle.GetBody(5);
            case BodySelection.AtIndex6:
                return kinectHandle.GetBody(6);
            case BodySelection.AtIndex7:
                return kinectHandle.GetBody(7);
        }
        return null;
    }

    void OnKinectBodiesChange()
    {
        var selectedBody = SelectBody();
        if (selectedBody == null) return;
        body = selectedBody;

        foreach (var hand in hands)
        {
            var center = body.GetAware(hand.center);
            var wrist = body.GetAware(hand.wrist);
            if (IsTracked(center) && IsTracked(wrist))
            {
                // when Kinect gives a new hand position
                hand.pos = (Vector3)center;
                var dif = (Vector3)wrist - (Vector3)hand.points[0];
                for (int i = 0; i < 21; i++)
                {
                    // we update all non-kinect point to move accordingly to Kinect info
                    hand.points[i] += new Vector4(dif.x, dif.y, dif.z, 0f);
                }
            }
        }
        RaiseBodyPointsChanged();
    }

    void DynamicConstrast(Texture texture)
    {
        Assert.IsTrue(texture.width == 512);
        Assert.IsTrue(texture.height == 512);

        // 256
        minMaxShader.SetTexture(0, "input", texture);
        minMaxShader.SetTexture(0, "output", minMax1);
        minMaxShader.Dispatch(0, 256 / 8, 256 / 8, 1);

        // 128
        minMaxShader.SetTexture(0, "input", minMax1);
        minMaxShader.SetTexture(0, "output", minMax2);
        minMaxShader.Dispatch(0, 128 / 8, 128 / 8, 1);

        // 64
        minMaxShader.SetTexture(0, "input", minMax2);
        minMaxShader.SetTexture(0, "output", minMax1);
        minMaxShader.Dispatch(0, 64 / 8, 64 / 8, 1);

        // 32
        minMaxShader.SetTexture(0, "input", minMax1);
        minMaxShader.SetTexture(0, "output", minMax2);
        minMaxShader.Dispatch(0, 32 / 8, 32 / 8, 1);

        // 16
        minMaxShader.SetTexture(0, "input", minMax2);
        minMaxShader.SetTexture(0, "output", minMax1);
        minMaxShader.Dispatch(0, 16 / 8, 16 / 8, 1);

        // 8
        minMaxShader.SetTexture(0, "input", minMax1);
        minMaxShader.SetTexture(0, "output", minMax2);
        minMaxShader.Dispatch(0, 8 / 8, 8 / 8, 1);

        // 4
        minMaxShader.SetTexture(0, "input", minMax2);
        minMaxShader.SetTexture(0, "output", minMax1);
        minMaxShader.Dispatch(0, 1, 1, 1);

        // 2
        minMaxShader.SetTexture(0, "input", minMax1);
        minMaxShader.SetTexture(0, "output", minMax2);
        minMaxShader.Dispatch(0, 1, 1, 1);

        // 1
        minMaxShader.SetTexture(0, "input", minMax2);
        minMaxShader.SetTexture(0, "output", minMax1);
        minMaxShader.Dispatch(0, 1, 1, 1);

        // apply dynamic contrast by dividing by the max value
        dynamicContrastShader.SetTexture(0, "result", texture);
        dynamicContrastShader.SetTexture(0, "max", minMax1);
        dynamicContrastShader.Dispatch(0, 512 / 8, 512 / 8, 1);
    }

    // when receiving new frame from video feed, launch HandPoseBarracuda analysis on it
    void HandAnalysis()
    {
        // reduce hand captation score over time, meaning, old data as lower score
        hands[0].score = Mathf.Max(hands[0].score - 0.1f, 0.7f);
        hands[1].score = Mathf.Max(hands[1].score - 0.1f, 0.7f);

        if (body == null) return;

        // if the previous computation is still ongoing, just don't start a new
        foreach (var hand in hands)
        {
            foreach (var pipeline in hand.pipelines)
            {
                if (pipeline.Busy) return;
            }
        }

        // one analysis is ran per hands and per input method
        // the effect of having several input methods in parallel, is a competitive captation
        // were we only take the one giving the better score
        foreach (var hand in hands)
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
                        cl.Crop(hand.pos, 0.18f, texture);
                        break;
                    case InputMode.Infrared:
                        ir.Crop(hand.pos, 0.18f, texture);
                        // on infrared, we have to apply a dynamic contrast filter
                        // it makes sure, the maximum value is always 1.0
                        DynamicConstrast(texture);
                        break;
                    case InputMode.Combined:
                        ir.Crop(hand.pos, 0.18f, texture);
                        DynamicConstrast(texture);
                        // on combined, we augment the infrared with color information from color video feed
                        colorizeShader.SetTexture(0, "color", cl.texture);
                        colorizeShader.SetVector("box", cl.BoundingBox(hand.pos, 0.18f));
                        colorizeShader.SetTexture(0, "result", texture);
                        colorizeShader.Dispatch(0, 512 / 8, 512 / 8, 1);
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
                    if (hand.index == 0 ? (h > 0.8 && h < 1.2) : (h > -0.2 && h < 0.2))
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

                            var wristAbs = body.Get(hand.wrist);
                            var wristRel = pipeline.GetWrist;
                            for (int i = 0; i < 21; i++)
                            {
                                // each point is positioned in Kinect space
                                var posRel = (Vector3)pipeline.HandPoints[i];
                                var point = (posRel - wristRel) * scale + wristAbs;
                                hand.points[i] = Tracked(point);
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
        kinectHandle.BodiesChanged -= OnKinectBodiesChange;
        if (cl != null)
        {
            cl.Changed -= HandAnalysis;
        }
        if (ir != null)
        {
            ir.Changed -= HandAnalysis;
        }
        foreach (var hand in hands)
        {
            foreach (var pipeline in hand.pipelines)
            {
                pipeline.Dispose();
            }
        }
    }

    // because, this class combines different body points detectors,
    // we need to pipe the request depending on what point is requested
    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (!availablePoints.ContainsKey(key))
        {
            return absent;
        }
        var (hand, at) = availablePoints[key];
        if (hand == 2)
        {
            if (body == null) return absent;
            return Tracked(body.Get(passThrough[at]));
        }
        else
        {
            return hands[hand].points[at];
        }
    }


    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();


    // the first int:
    // 0 = point is on left hand
    // 1 = point is on right hand
    // 2 = point is rest of body, handled by kinect
    // it would be much better to use algebraic data type instead of a (int, int), but saddly, not available
    // LeftHand(HandPoint)
    // RightHand(HandPoint)
    // Kinect()
    Dictionary<BodyPoint, (int, int)> availablePoints = new()
    {
        [BodyPoint.LeftWrist] = (0, 0),
        [BodyPoint.LeftThumb1] = (0, 1),
        [BodyPoint.LeftThumb2] = (0, 2),
        [BodyPoint.LeftThumb3] = (0, 3),
        [BodyPoint.LeftThumb] = (0, 4),
        [BodyPoint.LeftIndex1] = (0, 5),
        [BodyPoint.LeftIndex2] = (0, 6),
        [BodyPoint.LeftIndex3] = (0, 7),
        [BodyPoint.LeftIndex] = (0, 8),
        [BodyPoint.LeftMiddle1] = (0, 9),
        [BodyPoint.LeftMiddle2] = (0, 10),
        [BodyPoint.LeftMiddle3] = (0, 11),
        [BodyPoint.LeftMiddle] = (0, 12),
        [BodyPoint.LeftRing1] = (0, 13),
        [BodyPoint.LeftRing2] = (0, 14),
        [BodyPoint.LeftRing3] = (0, 15),
        [BodyPoint.LeftRing] = (0, 16),
        [BodyPoint.LeftPinky1] = (0, 17),
        [BodyPoint.LeftPinky2] = (0, 18),
        [BodyPoint.LeftPinky3] = (0, 19),
        [BodyPoint.LeftPinky] = (0, 20),

        [BodyPoint.RightWrist] = (1, 0),
        [BodyPoint.RightThumb1] = (1, 1),
        [BodyPoint.RightThumb2] = (1, 2),
        [BodyPoint.RightThumb3] = (1, 3),
        [BodyPoint.RightThumb] = (1, 4),
        [BodyPoint.RightIndex1] = (1, 5),
        [BodyPoint.RightIndex2] = (1, 6),
        [BodyPoint.RightIndex3] = (1, 7),
        [BodyPoint.RightIndex] = (1, 8),
        [BodyPoint.RightMiddle1] = (1, 9),
        [BodyPoint.RightMiddle2] = (1, 10),
        [BodyPoint.RightMiddle3] = (1, 11),
        [BodyPoint.RightMiddle] = (1, 12),
        [BodyPoint.RightRing1] = (1, 13),
        [BodyPoint.RightRing2] = (1, 14),
        [BodyPoint.RightRing3] = (1, 15),
        [BodyPoint.RightRing] = (1, 16),
        [BodyPoint.RightPinky1] = (1, 17),
        [BodyPoint.RightPinky2] = (1, 18),
        [BodyPoint.RightPinky3] = (1, 19),
        [BodyPoint.RightPinky] = (1, 20),

        [BodyPoint.Head] = (2, 0),
        [BodyPoint.Neck] = (2, 1),
        [BodyPoint.SpineShoulder] = (2, 2),
        [BodyPoint.LeftShoulder] = (2, 3),
        [BodyPoint.RightShoulder] = (2, 4),
        [BodyPoint.LeftElbow] = (2, 5),
        [BodyPoint.RightElbow] = (2, 6),
    };
    private static readonly JointType[] passThrough = new[]{
        JointType.Head,
        JointType.Neck,
        JointType.SpineShoulder,
        JointType.ShoulderLeft,
        JointType.ShoulderRight,
        JointType.ElbowLeft,
        JointType.ElbowRight,
    };
}
