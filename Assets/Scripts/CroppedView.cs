using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CroppedView : MonoBehaviour
{
    [SerializeField] KinectHandle kinect;
    [SerializeField] ComputeShader shader;
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] List<BodyPointsProvider.BodyPoint> bodyPoints;
    [SerializeField] float size = 2f;
    private RenderTexture texture;
    private List<AsyncGPUReadbackRequest> readBack;
    private Vector4 tracker;
    void Start()
    {
        readBack = new();
        tracker = new(0f, 0f, 1f, 1f);
        texture = new RenderTexture(224, 224, 0) { enableRandomWrite = true };
        GetComponent<MeshRenderer>().material.mainTexture = texture;
        kinect.ColorTextureChanged += OnNewInput;
        bodyPointsProvider.BodyPointsChanged += () =>
        {
            var pos = Vector3.zero;
            foreach (var bodyPoint in bodyPoints) {
                pos += (Vector3)bodyPointsProvider.GetBodyPoint(bodyPoint);
            }
            pos /= bodyPoints.Count;
            var hpos = new Vector2(pos.x, pos.z).normalized;
            var vpos = new Vector2(-pos.y, pos.z).normalized;
            var h = Mathf.Asin(hpos.x) / Mathf.PI * 180.0f;
            var v = Mathf.Asin(vpos.x) / Mathf.PI * 180.0f;
            var H = 84.1f;
            var V = 53.8f;
            tracker.z = size * 0.09f/pos.z;
            tracker.w = size * 0.16f/pos.z;
            tracker.x = (h + (H / 2f)) / H - tracker.z / 2f;
            tracker.y = (v + (V / 2f)) / V - tracker.w / 2f;
        };
    }

    void OnNewInput()
    {
        if (readBack.Count == 0)
        {
            shader.SetTexture(0, "input", kinect.ColorTexture);
            shader.SetVector("box", tracker);
            shader.SetTexture(0, "result", texture);
            shader.Dispatch(0, 224 / 8, 224 / 8, 1);
            readBack.Add(AsyncGPUReadback.Request(texture, 0, _ =>
            {
                readBack.Clear();
            }));
        }
    }

    void OnDestroy()
    {
        kinect.ColorTextureChanged -= OnNewInput;
        if (readBack.Count > 0)
        {
            readBack[0].WaitForCompletion();
        }
    }
}
