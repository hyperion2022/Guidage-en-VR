using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using Joint = Windows.Kinect.Joint;
using Vector4 = UnityEngine.Vector4;

public class BodyPointsFromKinect : BodyPointsProvider
{
    [SerializeField]
    KinectHandle kinect;

    void Start()
    {
        Assert.IsNotNull(kinect);
        kinect.BodiesChanged += RaiseBodyPointsChanged;
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

    private static readonly Dictionary<BodyPoint, JointType> availablePoints = new()
    {
        [BodyPoint.Head] = JointType.Head,
        [BodyPoint.LeftWrist] = JointType.WristLeft,
        [BodyPoint.RightWrist] = JointType.WristRight,
        [BodyPoint.LeftIndex] = JointType.HandTipLeft,
        [BodyPoint.RightIndex] = JointType.HandTipRight,
    };
    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (!availablePoints.ContainsKey(key)) return Vector4.zero;
        foreach (var body in kinect.Bodies) return JointToVec4(body.Joints[availablePoints[key]]);
        return Vector4.zero;
    }
    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();
}
