using UnityEngine;

// this is to try to crop the left and right hand from the kinect video feed, by using the kinect body points
public class KinectHandTrackingView: MonoBehaviour
{
    [SerializeField] KinectHandle kinect;

    void Start()
    {
        // cropped = new Texture2D(HandLandmarkDetector.ImageSize, HandLandmarkDetector.ImageSize, TextureFormat.RGBA32, false);
        // GetComponent<Renderer>().material.mainTexture = kinect.ColorTexture;
        kinect.ColorTextureChanged += () => GetComponent<Renderer>().material.mainTexture = kinect.ColorTexture;
    }
}
