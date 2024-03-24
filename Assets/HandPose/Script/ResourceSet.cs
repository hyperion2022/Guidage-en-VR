using UnityEngine;

// From Keijiro Takahashi
namespace MediaPipe
{
    [CreateAssetMenu(
        fileName = "ResourceSet",
        menuName = "ScriptableObjects/MediaPipe/HandPose Resource Set"
    )]
    public sealed class ResourceSet : ScriptableObject
    {
        public BlazePalm.ResourceSet blazePalm;
        public HandLandmark.ResourceSet handLandmark;
        public ComputeShader compute;
    }
}
