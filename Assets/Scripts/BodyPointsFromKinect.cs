using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using Joint = Windows.Kinect.Joint;
using Vector4 = UnityEngine.Vector4;

public class BodyPointsFromKinect : BodyPointsProvider
{
    [SerializeField]
    bool log = false;
    private KinectSensor kinect;
    private BodyFrameReader bodyReader;
    private Body[] bodies;

    // private BodyPoints bodyPoints = new BodyPoints{};
    // Start is called before the first frame update
    void Start()
    {
        kinect = KinectSensor.GetDefault();
        if (!kinect.IsOpen)
        {
            kinect.Open();
        }
        if (log)
        {
            Debug.Log("kinect sensor activated");
        }
        bodyReader = kinect.BodyFrameSource.OpenReader();
        Assert.IsNotNull(bodyReader);
        bodies = new Body[kinect.BodyFrameSource.BodyCount];
        bodyReader.FrameArrived += HandleFrame;
    }

    void HandleFrame(object _, BodyFrameArrivedEventArgs arg)
    {
        var frame = arg.FrameReference.AcquireFrame();
        Assert.IsNotNull(frame);
        frame.GetAndRefreshBodyData(bodies);
        // bodyPoints.head = JointToVec4(bodies[0].Joints[JointType.Head]);
        // bodyPoints.leftWrist = JointToVec4(bodies[0].Joints[JointType.WristLeft]);
        // bodyPoints.leftIndex = JointToVec4(bodies[0].Joints[JointType.HandTipLeft]);
        // bodyPoints.rightWrist = JointToVec4(bodies[0].Joints[JointType.WristRight]);
        // bodyPoints.rightIndex = JointToVec4(bodies[0].Joints[JointType.HandTipRight]);
        frame.Dispose();
        if (log) {
            Debug.Log("detected head position " + JointToVec4(bodies[0].Joints[JointType.Head]).ToString());
        }
        // BodyPointsUpdated();
        RaiseBodyPointsChanged();
    }

    private static Vector4 JointToVec4(Joint joint)
    {
        var w = joint.TrackingState switch
        {
            TrackingState.NotTracked => 3,
            TrackingState.Tracked => 1,
            TrackingState.Inferred => 2,
            _ => 1,
        };
        return new Vector4(joint.Position.X, joint.Position.Y, joint.Position.Z, w);
    }

    void OnDestroy()
    {
        bodyReader.Dispose();
        bodyReader = null;
        kinect.Close();
        kinect = null;
    }

    // public override BodyPoints GetBodyPoints() => bodyPoints;
    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (bodies[0] == null) return Vector4.zero;
        return key switch
        {
            BodyPoint.Head => JointToVec4(bodies[0].Joints[JointType.Head]),
            BodyPoint.LeftWrist => JointToVec4(bodies[0].Joints[JointType.WristLeft]),
            BodyPoint.RightWrist => JointToVec4(bodies[0].Joints[JointType.WristRight]),
            BodyPoint.LeftIndex => JointToVec4(bodies[0].Joints[JointType.HandTipLeft]),
            BodyPoint.RightIndex => JointToVec4(bodies[0].Joints[JointType.HandTipRight]),
            _ => Vector4.zero,
        };
    }
    public override BodyPoint[] AvailablePoints => new BodyPoint[] { BodyPoint.Head, BodyPoint.LeftWrist, BodyPoint.RightWrist, BodyPoint.LeftIndex, BodyPoint.RightIndex };
}
