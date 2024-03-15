using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using Vector4 = UnityEngine.Vector4;

public class BodyPointsFromKinect : BodyPointsProvider
{
    [SerializeField]
    KinectHandle kinect;

    void Start()
    {
        Assert.IsNotNull(kinect);
        kinect.OpenBody();
        kinect.BodiesChanged += RaiseBodyPointsChanged;
    }

    private static readonly Dictionary<BodyPoint, JointType> availablePoints = new()
    {
        [BodyPoint.Head] = JointType.Head,
        [BodyPoint.LeftWrist] = JointType.WristLeft,
        [BodyPoint.RightWrist] = JointType.WristRight,
        [BodyPoint.LeftIndex] = JointType.HandTipLeft,
        [BodyPoint.RightIndex] = JointType.HandTipRight,
        [BodyPoint.LeftThumb] = JointType.ThumbLeft,
        [BodyPoint.RightThumb] = JointType.ThumbRight,
    };
    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (!availablePoints.ContainsKey(key)) return absent;
        var tracked = kinect.TrackedBodies;
        if (tracked.Length == 0) return invalid;
        var body = kinect.GetBody(tracked[0]);
        if (body == null) return invalid;
        return body.GetAware(availablePoints[key]);
    }
    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();
}
