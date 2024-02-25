using UnityEngine;

namespace MediaPipe.HandPose {

//
// Public part of the hand pipeline class
//

partial class HandPipeline
{
    public Vector4[] GetKeyPoints() => ReadCache;
    public ComputeBuffer KeyPointBuffer
      => _buffer.filter;

    public ComputeBuffer HandRegionBuffer
      => _buffer.region;

    public ComputeBuffer HandRegionCropBuffer
      => _detector.landmark.InputBuffer;

    public bool UseAsyncReadback { get; set; } = true;

    public HandPipeline(ResourceSet resources)
      => AllocateObjects(resources);

    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture image)
      => RunPipeline(image);
    public delegate void BodyPointsUpdated();
    public event BodyPointsUpdated BodyPointsUpdatedEvent;
}

} // namespace MediaPipe.HandPose
