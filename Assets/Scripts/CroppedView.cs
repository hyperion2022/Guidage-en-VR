using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MediaPipe.HandLandmark;
using UnityEngine;
using UnityEngine.Rendering;

public class CroppedView : BodyPointsProvider
{
    [SerializeField] KinectHandle kinect;
    [SerializeField] ComputeShader shader;
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] List<BodyPoint> bodyPoints;
    [SerializeField] float size = 2f;
    [SerializeField] ResourceSet handLandmark;
    private RenderTexture texture;
    private Vector4 tracker;
    private HandLandmarkDetector landmark;

    readonly Dictionary<BodyPoint, HandLandmarkDetector.KeyPoint> availablePoints = new()
    {
        [BodyPoint.LeftWrist] = HandLandmarkDetector.KeyPoint.Wrist,
        [BodyPoint.LeftThumb1] = HandLandmarkDetector.KeyPoint.Thumb1,
        [BodyPoint.LeftThumb2] = HandLandmarkDetector.KeyPoint.Thumb2,
        [BodyPoint.LeftThumb3] = HandLandmarkDetector.KeyPoint.Thumb3,
        [BodyPoint.LeftThumb] = HandLandmarkDetector.KeyPoint.Thumb4,
        [BodyPoint.LeftIndex1] = HandLandmarkDetector.KeyPoint.Index1,
        [BodyPoint.LeftIndex2] = HandLandmarkDetector.KeyPoint.Index2,
        [BodyPoint.LeftIndex3] = HandLandmarkDetector.KeyPoint.Index3,
        [BodyPoint.LeftIndex] = HandLandmarkDetector.KeyPoint.Index4,
        [BodyPoint.LeftMiddle1] = HandLandmarkDetector.KeyPoint.Middle1,
        [BodyPoint.LeftMiddle2] = HandLandmarkDetector.KeyPoint.Middle2,
        [BodyPoint.LeftMiddle3] = HandLandmarkDetector.KeyPoint.Middle3,
        [BodyPoint.LeftMiddle] = HandLandmarkDetector.KeyPoint.Middle4,
        [BodyPoint.LeftRing1] = HandLandmarkDetector.KeyPoint.Ring1,
        [BodyPoint.LeftRing2] = HandLandmarkDetector.KeyPoint.Ring2,
        [BodyPoint.LeftRing3] = HandLandmarkDetector.KeyPoint.Ring3,
        [BodyPoint.LeftRing] = HandLandmarkDetector.KeyPoint.Ring4,
        [BodyPoint.LeftPinky1] = HandLandmarkDetector.KeyPoint.Pinky1,
        [BodyPoint.LeftPinky2] = HandLandmarkDetector.KeyPoint.Pinky2,
        [BodyPoint.LeftPinky3] = HandLandmarkDetector.KeyPoint.Pinky3,
        [BodyPoint.LeftPinky] = HandLandmarkDetector.KeyPoint.Pinky4,
    };
    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();
    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (!availablePoints.ContainsKey(key))
        {
            return absent;
        }
        var v = landmark.GetKeyPoint(availablePoints[key]);
        return new(v.x, v.y, 0f, 1f);
    }

    void Start()
    {
        landmark = new HandLandmarkDetector(handLandmark);
        tracker = new(0f, 0f, 1f, 1f);
        texture = new RenderTexture(224, 224, 0) { enableRandomWrite = true };
        GetComponent<MeshRenderer>().material.mainTexture = texture;
        kinect.ColorTextureChanged += OnNewInput;
        bodyPointsProvider.BodyPointsChanged += () =>
        {
            var pos = Vector3.zero;
            foreach (var bodyPoint in bodyPoints)
            {
                pos += (Vector3)bodyPointsProvider.GetBodyPoint(bodyPoint);
            }
            pos /= bodyPoints.Count;
            var hpos = new Vector2(pos.x, pos.z).normalized;
            var vpos = new Vector2(-pos.y, pos.z).normalized;
            var h = Mathf.Asin(hpos.x) / Mathf.PI * 180.0f;
            var v = Mathf.Asin(vpos.x) / Mathf.PI * 180.0f;
            var H = 84.1f;
            var V = 53.8f;
            tracker.z = size * 0.09f / pos.z;
            tracker.w = size * 0.16f / pos.z;
            tracker.x = (h + (H / 2f)) / H - tracker.z / 2f;
            tracker.y = (v + (V / 2f)) / V - tracker.w / 2f;
        };
    }

    void OnNewInput()
    {
        shader.SetTexture(0, "input", kinect.ColorTexture);
        shader.SetVector("box", tracker);
        shader.SetTexture(0, "result", texture);
        shader.Dispatch(0, 224 / 8, 224 / 8, 1);
        AsyncGPUReadback.Request(texture, 0, _ =>
        {
            landmark.ProcessImage(texture);
            AsyncGPUReadback.Request(landmark.OutputBuffer, _ => RaiseBodyPointsChanged());
        });
    }

    void OnDestroy()
    {
        landmark.Dispose();
        kinect.ColorTextureChanged -= OnNewInput;
    }
}
