using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using MediaPipe.HandPose;
using Vector4 = UnityEngine.Vector4;
using System.Linq;
using System.Collections.Generic;

public class BodyPointsFromBaracuda : BodyPointsProvider
{
    [SerializeField]
    bool log = false;
    [SerializeField] ResourceSet resources = null;
    private KinectSensor kinect;
    private ColorFrameReader colorReader;
    private byte[] rawImage;
    private Texture2D texture;

    HandPipeline pipeline;
    Vector4[] handPoints;

    Dictionary<BodyPoint, int> availablePoints = new Dictionary<BodyPoint, int>{
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
        handPoints = AvailablePoints.Select(_ => Vector4.zero).ToArray();
        pipeline = new HandPipeline(resources);
        kinect = KinectSensor.GetDefault();
        if (!kinect.IsOpen) {
            kinect.Open();
        }
        if (log) {
            Debug.Log("kinect sensor activated");
        }
        colorReader = kinect.ColorFrameSource.OpenReader();
        Assert.IsNotNull(colorReader);
        var desc = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        rawImage = new byte[desc.LengthInPixels * desc.BytesPerPixel];
        texture = new Texture2D(desc.Width, desc.Height, TextureFormat.RGBA32, false);
        colorReader.FrameArrived += HandleFrame;
        pipeline.BodyPointsUpdatedEvent += NewPoints;
    }

    void HandleFrame(object _, ColorFrameArrivedEventArgs arg) {
        var frame = arg.FrameReference.AcquireFrame();
        Assert.IsNotNull(frame);
        frame.CopyConvertedFrameDataToArray(rawImage, ColorImageFormat.Rgba);
        texture.LoadRawTextureData(rawImage);
        texture.Apply();
        frame.Dispose();
        pipeline.ProcessImage(texture);
    }

    void NewPoints() {
        handPoints = pipeline.HandPoints;
        RaiseBodyPointsChanged();
    }

    void OnDestroy() {
        colorReader.Dispose();
        colorReader = null;
        pipeline.Dispose();
        kinect.Close();
        kinect = null;
    }

    public override Vector4 GetBodyPoint(BodyPoint key) => availablePoints.ContainsKey(key) ? handPoints[availablePoints[key]] : Vector4.zero;
    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();
}
