using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using Vector4 = UnityEngine.Vector4;

// https://github.com/Kinect/Docs/blob/master/Kinect4Windows2.0/k4w2/Reference/Kinect_for_Windows_v2/Kinect/KinectSensor_Class.md

public class KinectHandle : MonoBehaviour
{
    [SerializeField] bool EnableLogs;
    [SerializeField] ComputeShader flipTexture;
    public bool IsAvailable => kinect.IsAvailable;
    public Body TrackedBody => (trackedBody >= 0) ? bodies[trackedBody] : null;
    public Texture ColorTexture => colorTexture;
    public Texture InfraredTexture => infraredTexture;
    public Vector2 ColorFov => colorFov;
    // public ComputeBuffer InfraredBuffer => infraredBuffer;
    public Vector2 InfraredFov => infraredFov;

    public event Action ColorTextureChanged;
    public event Action BodiesChanged;
    public event Action IsAvailableChanged;


    private KinectSensor kinect;
    private BodyFrameReader bodyFrameReader;
    private ColorFrameReader colorFrameReader;
    private LongExposureInfraredFrameReader infraredFrameReader;
    private Body[] bodies;
    private int trackedBody;
    private byte[] colorValues;
    private Texture2D colorTexture;
    // TODO: group using tuples
    private ushort[] infraredValues;
    // private float[] infraredValuesFloat;
    // private ComputeBuffer infraredBuffer;
    private Texture2D infraredTexture;
    private Vector2 infraredFov;
    private Vector2 colorFov;

    private bool init = true;
    void Start()
    {
        trackedBody = -1;
        kinect = KinectSensor.GetDefault();
        if (!kinect.IsOpen)
        {
            kinect.Open();
        }
        if (EnableLogs)
        {
            Debug.Log("Kinect Handle: Waiting availability ...");
            kinect.IsAvailableChanged += (_, _) => Debug.Log("Kinect Handle: " + (kinect.IsAvailable ? "Available" : "Not Available"));
        }
        bodyFrameReader = kinect.BodyFrameSource.OpenReader();
        colorFrameReader = kinect.ColorFrameSource.OpenReader();
        infraredFrameReader = kinect.LongExposureInfraredFrameSource.OpenReader();
        Assert.IsNotNull(bodyFrameReader);
        Assert.IsNotNull(colorFrameReader);
        Assert.IsNotNull(infraredFrameReader);
        var colorDesc = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        var infraredDesc = kinect.LongExposureInfraredFrameSource.FrameDescription;
        bodies = new Body[kinect.BodyFrameSource.BodyCount];
        colorValues = new byte[colorDesc.LengthInPixels * colorDesc.BytesPerPixel];
        infraredValues = new ushort[infraredDesc.Width * infraredDesc.Height];
        colorTexture = new Texture2D(colorDesc.Width, colorDesc.Height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
        };
        infraredTexture = new Texture2D(infraredDesc.Width, infraredDesc.Height, TextureFormat.R16, false);
        colorFov = new(colorDesc.HorizontalFieldOfView, colorDesc.VerticalFieldOfView);
        colorFov *= Mathf.PI / 180f;
        infraredFov = new(infraredDesc.HorizontalFieldOfView, infraredDesc.VerticalFieldOfView);
        infraredFov *= Mathf.PI / 180f;

        infraredFrameReader.FrameArrived += (_, arg) =>
        {
            var frame = arg.FrameReference.AcquireFrame();
            Assert.IsNotNull(frame);
            frame.CopyFrameDataToArray(infraredValues);
            frame.Dispose();
            infraredTexture.SetPixelData(infraredValues, 0);
            infraredTexture.Apply();
        };
        bodyFrameReader.FrameArrived += (_, arg) =>
        {
            var frame = arg.FrameReference.AcquireFrame();
            Assert.IsNotNull(frame);
            frame.GetAndRefreshBodyData(bodies);
            frame.Dispose();
            trackedBody = -1;
            foreach (var i in Enumerable.Range(0, bodies.Length))
            {
                if (bodies[i].IsTracked)
                {
                    trackedBody = i;
                    break;
                }
            }
            if (EnableLogs)
            {
                if (trackedBody >= 0)
                {
                    if (init)
                    {
                        Debug.Log($"Kinect Handle: Tracking state SUCCESS");
                        init = false;
                    }
                }
                else
                {
                    if (init)
                    {
                        Debug.Log($"Kinect Handle: Tracking state SEARCHING ...");
                    }
                    else
                    {
                        Debug.Log($"Kinect Handle: Tracking state LOST");
                        init = true;
                    }
                }
            }
            if (trackedBody >= 0)
            {
                BodiesChanged?.Invoke();
            }
        };
        colorFrameReader.FrameArrived += (_, arg) =>
        {
            // var infraredFrame = infraredFrameReader.AcquireLatestFrame();
            // if (infraredFrame != null) {
            //     Debug.Log($"Infrared: Frame arrived");
            //     infraredFrame.CopyFrameDataToArray(infraredValues);
            //     infraredBuffer.SetData(infraredValues);
            //     infraredFrame.Dispose();
            //     infraredFrame = null;
            // }
            var frame = arg.FrameReference.AcquireFrame();
            Assert.IsNotNull(frame);
            frame.CopyConvertedFrameDataToArray(colorValues, ColorImageFormat.Rgba);
            frame.Dispose();
            colorTexture.LoadRawTextureData(colorValues);
            colorTexture.Apply();
            ColorTextureChanged?.Invoke();
        };
        kinect.IsAvailableChanged += (_, arg) => IsAvailableChanged?.Invoke();
    }

    public static Vector3 ToVector3(Windows.Kinect.Joint joint) => new(joint.Position.X, joint.Position.Y, joint.Position.Z);

    public void FlippedColorTexture(RenderTexture destination)
    {
        Assert.IsTrue(colorTexture.width == destination.width);
        Assert.IsTrue(colorTexture.height == destination.height);
        flipTexture.SetInt("height", destination.height);
        flipTexture.SetTexture(0, "input", colorTexture);
        flipTexture.SetTexture(0, "output", destination);
        flipTexture.Dispatch(0, colorTexture.width / 8, colorTexture.height / 8, 1);
    }

    void OnDestroy()
    {
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }
        if (colorFrameReader != null)
        {
            colorFrameReader.Dispose();
            colorFrameReader = null;
        }
        if (infraredFrameReader != null)
        {
            infraredFrameReader.Dispose();
            infraredFrameReader = null;
        }
        if (kinect.IsOpen)
        {
            kinect.Close();
        }
        kinect = null;
    }
}
