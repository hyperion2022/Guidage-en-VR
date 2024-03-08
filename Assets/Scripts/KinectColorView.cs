using UnityEngine;
using UnityEngine.Assertions;

/// Displayrs on a quad, the video feed from the kinect, either from its color or infrared camera
public class KinectView : MonoBehaviour
{
    [SerializeField] KinectHandle kinect;
    [SerializeField] SourceType sourceType = SourceType.Color;

    public enum SourceType { Color, Infrared }
    KinectHandle.Source source;

    void Start()
    {
        Assert.IsNotNull(kinect);
        switch (sourceType)
        {
            case SourceType.Color:
                source = kinect.Cl;
                break;
            case SourceType.Infrared:
                source = kinect.Ir;
                break;
        }
        source.Changed += () => GetComponent<Renderer>().material.mainTexture = source.texture;
    }
}