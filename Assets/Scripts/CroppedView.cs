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
    [SerializeField] ComputeShader shaderOutput;
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] List<BodyPoint> bodyPoints;
    [SerializeField] float size = 2f;
    [SerializeField] ResourceSet handLandmark;

    private ComputeBuffer output;
    private Vector4 tracker;
    private HandLandmarkDetector landmark;

    private Vector4[] result;

    readonly Dictionary<BodyPoint, int> availablePoints = new()
    {
        [BodyPoint.LeftWrist] = 1,
        [BodyPoint.LeftThumb1] = 2,
        [BodyPoint.LeftThumb2] = 3,
        [BodyPoint.LeftThumb3] = 4,
        [BodyPoint.LeftThumb] = 5,
        [BodyPoint.LeftIndex1] = 6,
        [BodyPoint.LeftIndex2] = 7,
        [BodyPoint.LeftIndex3] = 8,
        [BodyPoint.LeftIndex] = 9,
        [BodyPoint.LeftMiddle1] = 10,
        [BodyPoint.LeftMiddle2] = 11,
        [BodyPoint.LeftMiddle3] = 12,
        [BodyPoint.LeftMiddle] = 13,
        [BodyPoint.LeftRing1] = 14,
        [BodyPoint.LeftRing2] = 15,
        [BodyPoint.LeftRing3] = 16,
        [BodyPoint.LeftRing] = 17,
        [BodyPoint.LeftPinky1] = 18,
        [BodyPoint.LeftPinky2] = 19,
        [BodyPoint.LeftPinky3] = 20,
        [BodyPoint.LeftPinky] = 21,
    };
    public override BodyPoint[] AvailablePoints => availablePoints.Keys.ToArray();
    public override Vector4 GetBodyPoint(BodyPoint key)
    {
        if (!availablePoints.ContainsKey(key))
        {
            return absent;
        }
        var v = result[availablePoints[key]];
        return new(v.x, v.y, v.z, 1f);
    }

    void Start()
    {
        output = new ComputeBuffer(22, 4 * sizeof(float));
        result = Enumerable.Repeat(Vector4.zero, 22).ToArray();
        landmark = new HandLandmarkDetector(handLandmark);
        tracker = new(0f, 0f, 1f, 1f);
        var go = transform.Find("InspectBaracudaInput");
        if (go != null) {
            var inspector = go.GetComponent<InspectBaracudaInput>();
            inspector.source = landmark.InputBuffer;
        }
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
            // Debug.Log($"Tracker at {tracker}");
        };
    }

    void OnNewInput()
    {
        Debug.Log("Tracker new input");
        shader.SetTexture(0, "input", kinect.ColorTexture);
        shader.SetVector("box", tracker);
        shader.SetBuffer(0, "output", landmark.InputBuffer);
        shader.Dispatch(0, 224 / 8, 224 / 8, 1);
        landmark.ProcessInput();
        shaderOutput.SetBuffer(0, "input", landmark.OutputBuffer);
        shaderOutput.SetBuffer(0, "output", output);
        shaderOutput.Dispatch(0, 1, 1, 1);
        AsyncGPUReadback.Request(output, 22 * 4 * sizeof(float), 0, req =>
        {
            req.GetData<Vector4>().CopyTo(result);
            Debug.Log($"Tracker finished {result[1]}");
            RaiseBodyPointsChanged();
        });
    }

    void OnDestroy()
    {
        landmark.Dispose();
        kinect.ColorTextureChanged -= OnNewInput;
    }
}
