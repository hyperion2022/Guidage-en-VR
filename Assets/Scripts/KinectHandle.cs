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
    public Body TrackedBody => (trackedBody >= 0) ? bodies[trackedBody] : null;

    public event Action ColorTextureChanged;
    public event Action BodiesChanged;
    public event Action IsAvailableChanged;


    private KinectSensor kinect;
    private BodyFrameReader bodyFrameReader;
    private ColorFrameReader colorFrameReader;
    private int bodyCount;
    private Body[] bodies;
    private int trackedBody;
    private byte[] colorBytes;
    private Texture2D colorTexture;
    void Start()
    {
        trackedBody = -1;
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
            trackedBody = -1;
            foreach (var i in Enumerable.Range(0, bodies.Length)) {
                if (bodies[i].IsTracked) {
                    trackedBody = i;
                    break;
                }
            }
            if (EnableLogs) {
                if (trackedBody >= 0) {
                    Debug.Log($"Kinect Handle: Received tracked body {trackedBody}");
                } else {
                    Debug.Log($"Kinect Handle: Failed to track body");
                }
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
