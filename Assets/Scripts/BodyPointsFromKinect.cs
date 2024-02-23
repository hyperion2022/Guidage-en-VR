using System.Collections;
using System.Collections.Generic;
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

    private BodyPoints bodyPoints = new BodyPoints{};
    // Start is called before the first frame update
    void Start()
    {
        kinect = KinectSensor.GetDefault();
        if (!kinect.IsOpen) {
            kinect.Open();
        }
        bodyReader = kinect.BodyFrameSource.OpenReader();
        Assert.IsNotNull(bodyReader);
        bodies = new Body[kinect.BodyFrameSource.BodyCount];
        bodyReader.FrameArrived += HandleFrame;
    }

    void HandleFrame(object _, BodyFrameArrivedEventArgs arg) {
        var frame = arg.FrameReference.AcquireFrame();
        Assert.IsNotNull(frame);
        frame.GetAndRefreshBodyData(bodies);
        bodyPoints.head = JointToVec4(bodies[0].Joints[JointType.Head]);
        bodyPoints.leftWrist = JointToVec4(bodies[0].Joints[JointType.WristLeft]);
        bodyPoints.leftIndex = JointToVec4(bodies[0].Joints[JointType.HandTipLeft]);
        bodyPoints.rightWrist = JointToVec4(bodies[0].Joints[JointType.WristRight]);
        bodyPoints.rightIndex = JointToVec4(bodies[0].Joints[JointType.HandTipRight]);
        frame.Dispose();
        if (log) {
            Debug.Log("detected head position" + bodyPoints.head.ToString());
        }
        EmitBodyPointsUpdatedEvent(bodyPoints);
    }

    private Vector4 JointToVec4(Joint joint) {
        var w = joint.TrackingState switch {
            TrackingState.NotTracked => 0,
            TrackingState.Tracked => 1,
            TrackingState.Inferred => 2,
            _ => 1,
        };
        return new Vector4(joint.Position.X, joint.Position.Y, joint.Position.Z, w);
    }

    void OnDestroy() {
        bodyReader.Dispose();
        bodyReader = null;
        kinect.Close();
        kinect = null;
    }

    public override BodyPoints GetBodyPoints() => bodyPoints;
}
