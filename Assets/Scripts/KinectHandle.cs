using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using Windows.Kinect;
using Vector4 = UnityEngine.Vector4;

// https://github.com/Kinect/Docs/blob/master/Kinect4Windows2.0/k4w2/Reference/Kinect_for_Windows_v2/Kinect/KinectSensor_Class.md

public class KinectHandle : BodyPointsProvider
{
    [SerializeField] bool EnableLogs;
    [SerializeField] ComputeShader flipShader;
    [SerializeField] ComputeShader cropShader;
    public bool IsAvailable => kinect.IsAvailable;

    public struct Body
    {
        public Windows.Kinect.Body body;

        public (PointState state, Vector3 pos) Get(JointType joint)
        {
            if (body == null) return (PointState.NotProvided, Vector3.zero);
            var pos = body.Joints[joint].Position;
            var state = body.Joints[joint].TrackingState switch
            {
                TrackingState.NotTracked => PointState.NotTracked,
                TrackingState.Tracked => PointState.Tracked,
                TrackingState.Inferred => PointState.Inferred,
                _ => PointState.NotProvided,
            };
            return (state, new(pos.X, pos.Y, pos.Z));
        }

        public static Body NotProvided = new(){ body = null };
    }
    public Body GetBody(int index)
    {
        if (index >= body.values.Length || index < 0) throw new IndexOutOfRangeException();
        return new Body { body = body.values[index] };
    }
    public int[] TrackedBodies => trackedBodies;
    private int[] trackedBodies;
    public event Action IsAvailableChanged;

    private (Source source, ColorFrameReader reader) cl;
    private (Source source, LongExposureInfraredFrameReader reader) ir;
    private KinectSensor kinect;
    private (BodyFrameReader reader, Windows.Kinect.Body[] values, bool[] tracked) body;

    public class Source
    {
        public ComputeShader cropShader;
        public ComputeShader flipShader;
        public Texture2D texture;
        public Vector2 fov;
        public Vector3 pov;
        public event Action Changed;

        public void Update() => Changed?.Invoke();

        public void Crop(Vector3 at, float radius, Texture destination)
        {
            cropShader.SetVector("box", BoundingBox(at, radius));
            cropShader.SetTexture(0, "input", texture);
            cropShader.SetTexture(0, "output", destination);
            cropShader.Dispatch(0, destination.width / 8, destination.height / 8, 1);
        }

        // From a kinect tracked position (relative to kinect camera)
        // finds where it lands on the camera image in normalized coordinates
        public Vector4 BoundingBox(Vector3 pos, float radius)
        {
            pos.y *= -1;
            var pos1 = pos - new Vector3(radius, radius, 0f);
            var pos2 = pos + new Vector3(radius, radius, 0f);
            var img1 = WorldToImage(pos1);
            var img2 = WorldToImage(pos2);
            return new Vector4(img1.x, img1.y, img2.x - img1.x, img2.y - img1.y);
        }
        public Vector2 WorldToImage(Vector3 pos)
        {
            pos -= pov;
            var nx = pos.x / pos.z;
            var ny = pos.y / pos.z;
            var hw = Mathf.Tan(fov.x / 2f);
            var vw = Mathf.Tan(fov.y / 2f);
            return new((nx / hw + 1f) / 2f, (ny / vw + 1f) / 2f);
        }
        public void Flip(Texture destination)
        {
            flipShader.SetTexture(0, "input", texture);
            flipShader.SetTexture(0, "output", destination);
            flipShader.Dispatch(0, destination.width / 8, destination.height / 8, 1);
        }
    }

    void Start()
    {
        Assert.IsNotNull(flipShader);
        Assert.IsNotNull(cropShader);
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
        OpenBody();
        trackedBodies = new int[] { };
        cl.source = null;
        ir.source = null;
        cl.reader = null;
        ir.reader = null;
        kinect.IsAvailableChanged += (_, arg) => IsAvailableChanged?.Invoke();
    }

    public Source Cl => GetCl();
    private Source GetCl()
    {
        if (cl.reader == null)
        {
            cl.reader = kinect.ColorFrameSource.OpenReader();
            Assert.IsNotNull(cl.reader);
            var desc = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            var values = new byte[desc.LengthInPixels * desc.BytesPerPixel];
            cl.source = new Source
            {
                texture = new Texture2D(desc.Width, desc.Height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, },
                fov = Mathf.PI / 180f * new Vector2(desc.HorizontalFieldOfView, desc.VerticalFieldOfView),
                pov = new Vector3(-0.0465f, +0.015f, 0f),
                cropShader = cropShader,
                flipShader = flipShader,
            };
            cl.reader.FrameArrived += (_, arg) =>
            {
                var frame = arg.FrameReference.AcquireFrame();
                frame.CopyConvertedFrameDataToArray(values, ColorImageFormat.Rgba);
                frame.Dispose();
                cl.source.texture.LoadRawTextureData(values);
                cl.source.texture.Apply();
                cl.source.Update();
            };
            Transform go = transform.Find($"Inspect Color");
            if (go != null)
            {
                go.GetComponent<MeshRenderer>().material.mainTexture = cl.source.texture;
            }
        }
        return cl.source;
    }

    public Source Ir => GetIr();
    public Source GetIr()
    {
        if (ir.reader == null)
        {
            ir.reader = kinect.LongExposureInfraredFrameSource.OpenReader();
            Assert.IsNotNull(ir.reader);
            var desc = kinect.LongExposureInfraredFrameSource.FrameDescription;
            var values = new ushort[desc.Width * desc.Height];
            ir.source = new Source
            {
                texture = new Texture2D(desc.Width, desc.Height, TextureFormat.R16, false) { wrapMode = TextureWrapMode.Clamp, },
                fov = Mathf.PI / 180f * new Vector2(desc.HorizontalFieldOfView, desc.VerticalFieldOfView),
                pov = Vector3.zero,
                cropShader = cropShader,
                flipShader = flipShader,
            };
            ir.reader.FrameArrived += (_, arg) =>
            {
                var frame = arg.FrameReference.AcquireFrame();
                frame.CopyFrameDataToArray(values);
                frame.Dispose();
                ir.source.texture.SetPixelData(values, 0);
                ir.source.texture.Apply();
                ir.source.Update();
            };
            Transform go = transform.Find($"Inspect Infrared");
            if (go != null)
            {
                go.GetComponent<MeshRenderer>().material.mainTexture = ir.source.texture;
            }
        }
        return ir.source;
    }

    private void OpenBody()
    {
        Assert.IsNull(body.reader);
        Assert.IsNotNull(kinect);
        body.reader = kinect.BodyFrameSource.OpenReader();
        Assert.IsNotNull(body.reader);
        body.values = new Windows.Kinect.Body[kinect.BodyFrameSource.BodyCount];
        body.tracked = body.values.Select(_ => false).ToArray();
        body.reader.FrameArrived += (_, arg) =>
        {
            var frame = arg.FrameReference.AcquireFrame();
            frame.GetAndRefreshBodyData(body.values);
            frame.Dispose();
            bool bodiesChanged = false;
            bool trackedChanged = false;
            for (int i = 0; i < body.values.Length; i++)
            {

                if (body.values[i].IsTracked)
                {
                    bodiesChanged = true;
                    if (!body.tracked[i])
                    {
                        body.tracked[i] = true;
                        trackedChanged = true;
                    }
                }
                else
                {
                    if (body.tracked[i])
                    {
                        body.tracked[i] = false;
                        trackedChanged = true;
                    }

                }
            }
            if (trackedChanged) trackedBodies = body.tracked
                .Select((v, i) => (v, i))
                .Where(p => p.v)
                .Select(p => p.i)
                .ToArray();
            if (trackedChanged && EnableLogs)
            {
                var bodies = JsonConvert.SerializeObject(trackedBodies);
                Debug.Log($"Kinect Handle: Tracking bodies {bodies}");
            }
            if (bodiesChanged) RaiseBodyPointsChanged();
        };
    }

    void OnDestroy()
    {
        body.reader.Dispose();
        body.reader = null;
        if (cl.reader != null)
        {
            cl.reader.Dispose();
            cl.reader = null;
        }
        if (ir.reader != null)
        {
            ir.reader.Dispose();
            ir.reader = null;
        }
        if (kinect.IsOpen)
        {
            kinect.Close();
        }
        kinect = null;
    }
    private static readonly Dictionary<BodyPoint, JointType> providedPoints = new()
    {
        [BodyPoint.Head] = JointType.Head,
        [BodyPoint.Neck] = JointType.Neck,
        [BodyPoint.SpineShoulder] = JointType.SpineShoulder,
        [BodyPoint.LeftShoulder] = JointType.ShoulderLeft,
        [BodyPoint.RightShoulder] = JointType.ShoulderRight,
        [BodyPoint.LeftElbow] = JointType.ElbowLeft,
        [BodyPoint.RightElbow] = JointType.ElbowRight,
        [BodyPoint.LeftWrist] = JointType.WristLeft,
        [BodyPoint.RightWrist] = JointType.WristRight,
        [BodyPoint.LeftIndex] = JointType.HandTipLeft,
        [BodyPoint.RightIndex] = JointType.HandTipRight,
        [BodyPoint.LeftIndex1] = JointType.HandLeft,
        [BodyPoint.RightIndex1] = JointType.HandRight,
        [BodyPoint.LeftThumb] = JointType.ThumbLeft,
        [BodyPoint.RightThumb] = JointType.ThumbRight,
    };
    public override (PointState, Vector3) GetBodyPoint(BodyPoint key)
    {
        if (!providedPoints.ContainsKey(key)) return (PointState.NotProvided, Vector3.zero);
        if (TrackedBodies.Length == 0) return (PointState.NotProvided, Vector3.zero);
        return GetBody(TrackedBodies[0]).Get(providedPoints[key]);
    }
    public override BodyPoint[] ProvidedPoints => providedPoints.Keys.ToArray();
}
