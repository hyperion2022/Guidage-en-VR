using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;

// https://github.com/Kinect/Docs/blob/master/Kinect4Windows2.0/k4w2/Reference/Kinect_for_Windows_v2/Kinect/KinectSensor_Class.md

public class KinectHandle : MonoBehaviour
{
    [SerializeField] bool EnableLogs;
    [SerializeField] ComputeShader flipTexture;
    public bool IsAvailable => kinect.IsAvailable;
    public Body TrackedBody => (body.tracked >= 0) ? body.values[body.tracked] : null;
    public Texture ColorTexture => color.texture;
    public Texture InfraredTexture => infrared.texture;
    public Vector2 ColorFov => color.fov;
    public Vector2 InfraredFov => infrared.fov;

    public event Action ColorTextureChanged;
    public event Action InfraredTextureChanged;
    public event Action BodiesChanged;
    public event Action IsAvailableChanged;


    private KinectSensor kinect;

    private (BodyFrameReader reader, Body[] values, int tracked) body;
    private (Vector3 fov, Texture2D texture, byte[] values, ColorFrameReader reader) color;
    private (Vector3 fov, Texture2D texture, ushort[] values, LongExposureInfraredFrameReader reader) infrared;

    private bool init = true;
    void Start()
    {
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
        body.reader = null;
        body.tracked = -1;
        color.reader = null;
        color.texture = null;
        color.fov = Vector2.one;
        infrared.reader = null;
        infrared.texture = null;
        infrared.fov = Vector2.one;
        kinect.IsAvailableChanged += (_, arg) => IsAvailableChanged?.Invoke();
    }

    public void OpenColor()
    {
        if (color.reader != null) return;
        color.reader = kinect.ColorFrameSource.OpenReader();
        Assert.IsNotNull(color.reader);
        var colorDesc = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        color.values = new byte[colorDesc.LengthInPixels * colorDesc.BytesPerPixel];
        color.texture = new Texture2D(colorDesc.Width, colorDesc.Height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
        };
        color.fov = new(colorDesc.HorizontalFieldOfView, colorDesc.VerticalFieldOfView);
        color.fov *= Mathf.PI / 180f;
        color.reader.FrameArrived += (_, arg) =>
        {
            var frame = arg.FrameReference.AcquireFrame();
            frame.CopyConvertedFrameDataToArray(color.values, ColorImageFormat.Rgba);
            frame.Dispose();
            color.texture.LoadRawTextureData(color.values);
            color.texture.Apply();
            ColorTextureChanged?.Invoke();
        };
    }

    public void OpenInfrared()
    {
        if (infrared.reader != null) return;
        infrared.reader = kinect.LongExposureInfraredFrameSource.OpenReader();
        Assert.IsNotNull(infrared.reader);
        var infraredDesc = kinect.LongExposureInfraredFrameSource.FrameDescription;
        infrared.values = new ushort[infraredDesc.Width * infraredDesc.Height];
        infrared.texture = new Texture2D(infraredDesc.Width, infraredDesc.Height, TextureFormat.R16, false)
        {
            wrapMode = TextureWrapMode.Clamp,
        };
        infrared.fov = new(infraredDesc.HorizontalFieldOfView, infraredDesc.VerticalFieldOfView);
        infrared.fov *= Mathf.PI / 180f;

        infrared.reader.FrameArrived += (_, arg) =>
        {
            var frame = arg.FrameReference.AcquireFrame();
            frame.CopyFrameDataToArray(infrared.values);
            frame.Dispose();
            infrared.texture.SetPixelData(infrared.values, 0);
            infrared.texture.Apply();
            InfraredTextureChanged?.Invoke();
        };
    }

    public void OpenBody()
    {
        if (body.reader != null) return;
        body.tracked = -1;
        Assert.IsNotNull(kinect);
        body.reader = kinect.BodyFrameSource.OpenReader();
        Assert.IsNotNull(body.reader);
        body.values = new Body[kinect.BodyFrameSource.BodyCount];
        body.reader.FrameArrived += (_, arg) =>
        {
            var frame = arg.FrameReference.AcquireFrame();
            frame.GetAndRefreshBodyData(body.values);
            frame.Dispose();
            body.tracked = -1;
            foreach (var i in Enumerable.Range(0, body.values.Length))
            {
                if (body.values[i].IsTracked)
                {
                    body.tracked = i;
                    break;
                }
            }
            if (EnableLogs)
            {
                if (body.tracked >= 0)
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
            if (body.tracked >= 0)
            {
                BodiesChanged?.Invoke();
            }
        };
    }

    public static Vector3 ToVector3(Windows.Kinect.Joint joint) => new(joint.Position.X, joint.Position.Y, joint.Position.Z);

    public void FlippedColorTexture(RenderTexture destination)
    {
        Assert.IsTrue(color.texture.width == destination.width);
        Assert.IsTrue(color.texture.height == destination.height);
        flipTexture.SetInt("height", destination.height);
        flipTexture.SetTexture(0, "input", color.texture);
        flipTexture.SetTexture(0, "output", destination);
        flipTexture.Dispatch(0, color.texture.width / 8, color.texture.height / 8, 1);
    }

    void OnDestroy()
    {
        if (body.reader != null)
        {
            body.reader.Dispose();
            body.reader = null;
        }
        if (color.reader != null)
        {
            color.reader.Dispose();
            color.reader = null;
        }
        if (infrared.reader != null)
        {
            infrared.reader.Dispose();
            infrared.reader = null;
        }
        if (kinect.IsOpen)
        {
            kinect.Close();
        }
        kinect = null;
    }
}
