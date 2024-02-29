using System;
using System.Linq;
using MediaPipe.BlazePalm;
using MediaPipe.HandLandmark;
using UnityEngine;
using UnityEngine.Rendering;

namespace MediaPipe.HandPose
{
    sealed class HandPipeline : System.IDisposable
    {
        public Vector4[] HandPoints = Enumerable.Repeat(Vector4.zero, KeyPointCount).ToArray();
        public const int KeyPointCount = 21;
        // public enum KeyPoint {
        //     Wrist,
        //     Thumb1, Thumb2, Thumb3, Thumb4,
        //     Index1, Index2, Index3, Index4,
        //     Middle1, Middle2, Middle3, Middle4,
        //     Ring1, Ring2, Ring3, Ring4,
        //     Pinky1, Pinky2, Pinky3, Pinky4
        // }

        readonly ResourceSet resourceSet;

        // this detect where the hand is on the image
        readonly PalmDetector palm;
        public readonly ComputeBuffer HandRegionBuffer;

        // this detect the hand points
        readonly HandLandmarkDetector landmark;
        public readonly ComputeBuffer KeyPointBuffer;
        public readonly ComputeBuffer StatBuffer;
        public Vector4[] Stat;


        public ComputeBuffer HandRegionCropBuffer => landmark.InputBuffer;

        public HandPipeline(ResourceSet resources)
        {
            resourceSet = resources;

            palm = new PalmDetector(resourceSet.blazePalm);
            landmark = new HandLandmarkDetector(resourceSet.handLandmark);

            var regionStructSize = sizeof(float) * 24;
            var filterBufferLength = HandLandmarkDetector.VertexCount * 2;

            HandRegionBuffer = new ComputeBuffer(1, regionStructSize);
            KeyPointBuffer = new ComputeBuffer(filterBufferLength, sizeof(float) * 4);
            StatBuffer = new ComputeBuffer(1, 4 * sizeof(float));
            Stat = new[] { Vector4.zero };

            Shader.SetKeyword(GlobalKeyword.Create("NCHW_INPUT"), palm.InputIsNCHW);
        }

        public void Dispose()
        {
            palm.Dispose();
            landmark.Dispose();
            HandRegionBuffer.Dispose();
            KeyPointBuffer.Dispose();
        }

        public float Score => Stat[0].x;
        public float Handedness => Stat[0].y;

        public void ProcessImage(Texture input)
        {
            var cs = resourceSet.compute;

            // Letterboxing scale factor
            var scale = new Vector2
              (Mathf.Max((float)input.height / input.width, 1f),
               Mathf.Max((float)input.width / input.height, 1f));

            // Image scaling and padding
            cs.SetInt("_spad_width", palm.ImageSize);
            cs.SetVector("_spad_scale", scale);
            cs.SetTexture(0, "_spad_input", input);
            cs.SetBuffer(0, "_spad_output", palm.InputBuffer);
            cs.Dispatch(0, palm.ImageSize / 8, palm.ImageSize / 8, 1);

            // Palm detection
            palm.ProcessInput();

            // Hand region bounding box update
            cs.SetFloat("_bbox_dt", Time.deltaTime);
            cs.SetBuffer(1, "_bbox_count", palm.CountBuffer);
            cs.SetBuffer(1, "_bbox_palm", palm.DetectionBuffer);
            cs.SetBuffer(1, "_bbox_region", HandRegionBuffer);
            cs.Dispatch(1, 1, 1, 1);

            // Hand region cropping
            cs.SetTexture(2, "_crop_input", input);
            cs.SetBuffer(2, "_crop_region", HandRegionBuffer);
            cs.SetBuffer(2, "_crop_output", landmark.InputBuffer);
            cs.Dispatch(2, HandLandmarkDetector.ImageSize / 8, HandLandmarkDetector.ImageSize / 8, 1);

            // Hand landmark detection
            landmark.ProcessInput();

            cs.SetBuffer(4, "_stat_input", landmark.OutputBuffer);
            cs.SetBuffer(4, "_stat_output", StatBuffer);
            cs.Dispatch(4, 1, 1, 1);

            // Key point postprocess
            cs.SetFloat("_post_dt", Time.deltaTime);
            cs.SetFloat("_post_scale", scale.y);
            cs.SetBuffer(3, "_post_input", landmark.OutputBuffer);
            cs.SetBuffer(3, "_post_region", HandRegionBuffer);
            cs.SetBuffer(3, "_post_output", KeyPointBuffer);
            cs.Dispatch(3, 1, 1, 1);

            AsyncGPUReadback.Request(KeyPointBuffer, KeyPointCount * sizeof(float) * 4, 0, req =>
            {
                req.GetData<Vector4>().CopyTo(HandPoints);
                StatBuffer.GetData(Stat);
                BodyPointsUpdatedEvent?.Invoke();
            });
        }
        public event Action BodyPointsUpdatedEvent;

        public Vector3 GetWrist => HandPoints[0];
        public Vector3 GetIndex1 => HandPoints[5];

    }
}
