using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using MediaPipe.HandPose;
using Vector4 = UnityEngine.Vector4;

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

    void Start()
    {
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

    void OnDestroy() {
        colorReader.Dispose();
        colorReader = null;
        pipeline.Dispose();
        kinect.Close();
        kinect = null;
    }

    // public override BodyPoints GetBodyPoints() {
    //     var bodyPoints = BodyPoints.empty;
    //     var handPoints = pipeline.GetKeyPoints();
    //     bodyPoints.leftWrist = handPoints[(int)HandProvider.KeyPoint.Wrist];
    //     bodyPoints.leftWrist.w = 1f;
    //     bodyPoints.leftIndex = handPoints[(int)HandProvider.KeyPoint.Index4];
    //     bodyPoints.leftIndex.w = 1f;
    //     bodyPoints.leftThumb = handPoints[(int)HandProvider.KeyPoint.Thumb4];
    //     bodyPoints.leftThumb.w = 1f;
    //     return bodyPoints;
    // }
    public override Vector4 GetBodyPoint(Key key)
    {
        var handPoints = pipeline.GetKeyPoints();
        return key switch {
            Key.LeftWrist => handPoints[0],
            Key.LeftIndex => handPoints[8],
            Key.LeftThumb => handPoints[4],
            _ => Vector4.zero,
        };
    }
    public override Key[] AvailablePoints => new Key[]{Key.LeftWrist, Key.LeftIndex, Key.LeftThumb};
}
