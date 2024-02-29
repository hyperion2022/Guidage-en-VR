using UnityEngine;
using MediaPipe.HandPose;
using Vector4 = UnityEngine.Vector4;
using System.Linq;
using System.Collections.Generic;

public class BodyPointsFromBaracuda : BodyPointsProvider
{
    [SerializeField]
    KinectHandle kinect;
    [SerializeField] ResourceSet resources = null;
    private HandPipeline pipeline;

    Dictionary<BodyPoint, int> availablePoints = new Dictionary<BodyPoint, int>
    {
        [BodyPoint.LeftWrist] = 0,
        [BodyPoint.LeftThumb1] = 1,
        [BodyPoint.LeftThumb2] = 2,
        [BodyPoint.LeftThumb3] = 3,
        [BodyPoint.LeftThumb] = 4,
        [BodyPoint.LeftIndex1] = 5,
        [BodyPoint.LeftIndex2] = 6,
        [BodyPoint.LeftIndex3] = 7,
        [BodyPoint.LeftIndex] = 8,
        [BodyPoint.LeftMiddle1] = 9,
        [BodyPoint.LeftMiddle2] = 10,
        [BodyPoint.LeftMiddle3] = 11,
        [BodyPoint.LeftMiddle] = 12,
        [BodyPoint.LeftRing1] = 13,
        [BodyPoint.LeftRing2] = 14,
        [BodyPoint.LeftRing3] = 15,
        [BodyPoint.LeftRing] = 16,
        [BodyPoint.LeftPinky1] = 17,
        [BodyPoint.LeftPinky2] = 18,
        [BodyPoint.LeftPinky3] = 19,
        [BodyPoint.LeftPinky] = 20,
    };

    void Start()
    {
        pipeline = new HandPipeline(resources);
        kinect.ColorTextureChanged += () => pipeline.ProcessImage(kinect.ColorTexture);
        pipeline.BodyPointsUpdatedEvent += RaiseBodyPointsChanged;

        var go = transform.Find("InspectBaracudaInput");
        if (go != null) {
            var inspect = go.GetComponent<InspectBaracudaInput>();
            inspect.source = pipeline.HandRegionCropBuffer;
        }
    }

    void OnDestroy()
    {
        pipeline.Dispose();
    }

    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (!availablePoints.ContainsKey(key))
        {
            return absent;
        }
        var v = pipeline.HandPoints[availablePoints[key]];
        v.Scale(new(1f, -1f, 1f, 1f));
        return v;
    }
    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();
}
