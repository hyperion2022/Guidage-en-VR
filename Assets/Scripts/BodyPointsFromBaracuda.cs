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

    Dictionary<Key, int> availablePoints = new Dictionary<Key, int>{
        [Key.LeftWrist] = 0,
        [Key.LeftThumb1] = 1,
        [Key.LeftThumb2] = 2,
        [Key.LeftThumb3] = 3,
        [Key.LeftThumb] = 4,
        [Key.LeftIndex1] = 5,
        [Key.LeftIndex2] = 6,
        [Key.LeftIndex3] = 7,
        [Key.LeftIndex] = 8,
        [Key.LeftMiddle1] = 9,
        [Key.LeftMiddle2] = 10,
        [Key.LeftMiddle3] = 11,
        [Key.LeftMiddle] = 12,
        [Key.LeftRing1] = 13,
        [Key.LeftRing2] = 14,
        [Key.LeftRing3] = 15,
        [Key.LeftRing] = 16,
        [Key.LeftPinky1] = 17,
        [Key.LeftPinky2] = 18,
        [Key.LeftPinky3] = 19,
        [Key.LeftPinky] = 20,
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
        EmitBodyPointsUpdatedEvent();
    }

    void OnDestroy() {
        colorReader.Dispose();
        colorReader = null;
        pipeline.Dispose();
        kinect.Close();
        kinect = null;
    }

    public override Vector4 GetBodyPoint(Key key) => availablePoints.ContainsKey(key) ? handPoints[availablePoints[key]] : Vector4.zero;
    public override Key[] AvailablePoints => availablePoints.Keys.ToArray();
}
