using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using MediaPipe.HandLandmark;

// this is to try to crop the left and right hand from the kinect video feed, by using the kinect body points
public class KinectHandTrackingView: MonoBehaviour
{
    private KinectSensor kinect;
    private ColorFrameReader colorReader;
    private byte[] rawImage;
    private Texture2D texture;
    private Texture2D cropped;


    void Start()
    {
        kinect = KinectSensor.GetDefault();
        if (!kinect.IsOpen) {
            kinect.Open();
        }
        colorReader = kinect.ColorFrameSource.OpenReader();
        Assert.IsNotNull(colorReader);
        var desc = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        rawImage = new byte[desc.LengthInPixels * desc.BytesPerPixel];
        texture = new Texture2D(desc.Width, desc.Height, TextureFormat.RGBA32, false);
        cropped = new Texture2D(HandLandmarkDetector.ImageSize, HandLandmarkDetector.ImageSize, TextureFormat.RGBA32, false);
        colorReader.FrameArrived += HandleFrame;
        GetComponent<Renderer>().material.mainTexture = texture;
    }

    void HandleFrame(object _, ColorFrameArrivedEventArgs arg) {
        var frame = arg.FrameReference.AcquireFrame();
        Assert.IsNotNull(frame);
        frame.CopyConvertedFrameDataToArray(rawImage, ColorImageFormat.Rgba);
        texture.LoadRawTextureData(rawImage);
        texture.Apply();
        frame.Dispose();
    }

    void OnDestroy() {
        colorReader.Dispose();
        colorReader = null;
        kinect.Close();
        kinect = null;
    }
}
