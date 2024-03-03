using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.HandPose;
using UnityEngine.Assertions;

public sealed class HandVisualizer : MonoBehaviour
{
    [SerializeField] KinectHandle kinect;
    [Space]
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] Shader _keyPointShader = null;
    [SerializeField] Shader _handRegionShader = null;
    [Space]
    [SerializeField] RawImage _mainUI = null;
    [SerializeField] RawImage _cropUI = null;

    HandPipeline _pipeline;
    (Material keys, Material region) _material;
    RenderTexture texture;
    KinectHandle.Source color;

    void Start()
    {
        texture = new RenderTexture(1920, 1080, 0) { enableRandomWrite = true };
        _pipeline = new HandPipeline(_resources);
        _material = (new Material(_keyPointShader), new Material(_handRegionShader));
        _material.keys.SetBuffer("_KeyPoints", _pipeline.KeyPointBuffer);
        _material.region.SetBuffer("_Image", _pipeline.HandRegionCropBuffer);
        _cropUI.material = _material.region;
        color = kinect.Cl;
    }

    void OnDestroy()
    {
        _pipeline.Dispose();
        Destroy(_material.keys);
        Destroy(_material.region);
    }

    void LateUpdate()
    {
        color.Flip(texture);
        _pipeline.ProcessImage(texture);
        _mainUI.texture = texture;
        _cropUI.texture = texture;
    }

    void OnRenderObject()
    {
        _material.keys.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 96, 21);
        _material.keys.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 2, 4 * 5 + 1);
    }
}
