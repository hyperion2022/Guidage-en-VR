using MediaPipe.HandPose;
using UnityEngine;
using JointType = Windows.Kinect.JointType;
using System.Linq;
using System.Collections.Generic;

public class AdvancedTracking : BodyPointsProvider
{
    [SerializeField] KinectHandle kinectHandle;
    [SerializeField] ResourceSet resourceSet;
    [SerializeField] ComputeShader computeShader;

    private struct Hand {
        public int index;
        public HandPipeline pipeline;
        public Vector4 box;
        public JointType center;
        public JointType wrist;
        public RenderTexture texture;
        public Vector4[] points;
    }
    private Hand[] hands;

    void Start()
    {
        hands = new Hand[]{
            new(){
                index = 0,
                pipeline = new(resourceSet),
                box = new(0f, 0f, 1f, 1f),
                center = JointType.HandLeft,
                wrist = JointType.WristLeft,
                texture = new(512, 512, 0){enableRandomWrite = true},
                points = Enumerable.Repeat(absent, 21).ToArray(),
            },
            new(){
                index = 1,
                pipeline = new(resourceSet),
                box = new(0f, 0f, 1f, 1f),
                center = JointType.HandRight,
                wrist = JointType.WristRight,
                texture = new(512, 512, 0){enableRandomWrite = true},
                points = Enumerable.Repeat(absent, 21).ToArray(),
            },
        };
        kinectHandle.BodiesChanged += OnKinectBodiesChange;
        kinectHandle.ColorTextureChanged += OnKinectColorChange;

        Transform go;
        go = transform.Find("Inspect0");
        if (go != null)
        {
            go.GetComponent<InspectBaracudaInput>().source = hands[0].pipeline.HandRegionCropBuffer;
        }
        go = transform.Find("Inspect1");
        if (go != null)
        {
            go.GetComponent<InspectBaracudaInput>().source = hands[1].pipeline.HandRegionCropBuffer;
        }
    }


    // From a kinect tracked position (relative to kinect camera)
    // finds where it lands on the camera image in normalized coordinates
    private static Vector4 BoundingBox(Vector3 pos)
    {
        var hpos = new Vector2(pos.x, pos.z).normalized;
        var vpos = new Vector2(-pos.y, pos.z).normalized;
        var h = Mathf.Asin(hpos.x) / Mathf.PI * 180.0f;
        var v = Mathf.Asin(vpos.x) / Mathf.PI * 180.0f;
        var H = 84.1f; // kinect horizontal field of view in degrees
        var V = 53.8f; // kinect vertical field of view in degrees

        var boundingBox = Vector4.zero;
        boundingBox.z = 2f * 0.09f / pos.z;
        boundingBox.w = 2f * 0.16f / pos.z;
        boundingBox.x = (h + (H / 2f)) / H - boundingBox.z / 2f;
        boundingBox.y = (v + (V / 2f)) / V - boundingBox.w / 2f;
        return boundingBox;
    }

    void OnKinectBodiesChange()
    {
        var body = kinectHandle.TrackedBody;
        if (body == null) return;
        hands[0].box = BoundingBox(KinectHandle.ToVector3(body.Joints[hands[0].center]));
        hands[1].box = BoundingBox(KinectHandle.ToVector3(body.Joints[hands[1].center]));
    }

    void OnKinectColorChange()
    {
        var body = kinectHandle.TrackedBody;
        if (body == null) return;
        foreach (var hand in hands)
        {
            computeShader.SetTexture(0, "input", kinectHandle.ColorTexture);
            computeShader.SetTexture(0, "output", hand.texture);
            computeShader.SetVector("box", hand.box);
            computeShader.Dispatch(0, 512 / 8, 512 / 8, 1);
            hand.pipeline.ProcessImage(hand.texture);

            hand.pipeline.BodyPointsUpdatedEvent += () =>
            {
                var h = hand.pipeline.Handedness;
                if (hand.index == 0 ? (h > 0.8 && h < 1.2) : (h > -0.2 && h < 0.2))
                {
                    var ref1 = hand.pipeline.GetWrist;
                    var ref2 = hand.pipeline.GetIndex1;
                    var dist = Vector3.Distance(ref1, ref2);
                    if (dist > 0.1f && dist < 4f) {
                        var scale = 0.11f / dist;
                        var wristAbs = KinectHandle.ToVector3(body.Joints[hand.wrist]);
                        var wristRel = hand.pipeline.GetWrist;
                        for (int i = 0; i < 21; i++)
                        {
                            var posRel = (Vector3)hand.pipeline.HandPoints[i];
                            var point = (posRel - wristRel) * scale + wristAbs;
                            hand.points[i] = new Vector4(point.x, point.y, point.z, 1f);
                        }
                        RaiseBodyPointsChanged();
                    }
                }
            };
        }
    }

    void OnDestroy()
    {
        kinectHandle.BodiesChanged -= OnKinectBodiesChange;
        kinectHandle.ColorTextureChanged -= OnKinectColorChange;
        hands[0].pipeline.Dispose();
        hands[1].pipeline.Dispose();
    }

    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (!availablePoints.ContainsKey(key))
        {
            return absent;
        }
        var (hand, at) = availablePoints[key];
        if (hand == 2)
        {
            var body = kinectHandle.TrackedBody;
            if (body == null) return absent;
            var pos = body.Joints[passThrough[at]].Position;
            return new Vector4(pos.X, pos.Y, pos.Z, 1f);
        }
        else
        {
            return hands[hand].points[at];
        }
    }


    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();


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