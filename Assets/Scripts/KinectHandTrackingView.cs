using UnityEngine;

public class KinectColorView: MonoBehaviour
{
    [SerializeField] KinectHandle kinect;

    void Start()
    {
        kinect.ColorTextureChanged += () => GetComponent<Renderer>().material.mainTexture = kinect.ColorTexture;
    }
}
