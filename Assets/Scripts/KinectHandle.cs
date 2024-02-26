using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;

// https://github.com/Kinect/Docs/blob/master/Kinect4Windows2.0/k4w2/Reference/Kinect_for_Windows_v2/Kinect/KinectSensor_Class.md

public class KinectHandle : MonoBehaviour
{
    [SerializeField] bool EnableLogs;
    public bool IsAvailable => kinect.IsAvailable;
    public Texture ColorTexture => colorTexture;
    public IEnumerable<Body> Bodies => bodies.Take(bodyCount);

    public event Action ColorTextureChanged;
    public event Action BodiesChanged;
    public event Action IsAvailableChanged;


    private KinectSensor kinect;
    private BodyFrameReader bodyFrameReader;
    private ColorFrameReader colorFrameReader;
    private int bodyCount;
    private Body[] bodies;
    private byte[] colorBytes;
    private Texture2D colorTexture;
    void Start()
    {
        kinect = KinectSensor.GetDefault();
        if (EnableLogs) {
            kinect.IsAvailableChanged += (_, _) => Debug.Log("Kinect Handle: " + (kinect.IsAvailable ? "Available" : "Not Available"));
        }
        if (!kinect.IsOpen)
        {
            kinect.Open();
        }
        bodyFrameReader = kinect.BodyFrameSource.OpenReader();
        colorFrameReader = kinect.ColorFrameSource.OpenReader();
        Assert.IsNotNull(bodyFrameReader);
        Assert.IsNotNull(colorFrameReader);
        bodyCount = 0;
        bodies = new Body[kinect.BodyFrameSource.BodyCount];
        var desc = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        colorBytes = new byte[desc.LengthInPixels * desc.BytesPerPixel];
        colorTexture = new Texture2D(desc.Width, desc.Height, TextureFormat.RGBA32, false);

        bodyFrameReader.FrameArrived += (_, arg) =>
        {
            var frame = arg.FrameReference.AcquireFrame();
            Assert.IsNotNull(frame);
            bodyCount = frame.BodyCount;
            frame.GetAndRefreshBodyData(bodies);
            frame.Dispose();
            if (EnableLogs) {
                Debug.Log($"Kinect Handle: Received {bodyCount} bodies");
            }
            BodiesChanged?.Invoke();
        };
        colorFrameReader.FrameArrived += (_, arg) => {
            var frame = arg.FrameReference.AcquireFrame();
            Assert.IsNotNull(frame);
            frame.CopyConvertedFrameDataToArray(colorBytes, ColorImageFormat.Rgba);
            frame.Dispose();
            colorTexture.LoadRawTextureData(colorBytes);
            colorTexture.Apply();
            ColorTextureChanged?.Invoke();
        };
        kinect.IsAvailableChanged += (_, arg) => IsAvailableChanged?.Invoke();
    }

    void OnDestroy()
    {
        bodyFrameReader.Dispose();
        bodyFrameReader = null;
        colorFrameReader.Dispose();
        colorFrameReader = null;
        if (kinect.IsOpen)
        {
            kinect.Close();
        }
        kinect = null;
    }
}
