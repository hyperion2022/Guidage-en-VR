using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UserOnboarding
{
    public class HandTracking
    {
        public Vector4[] HandPoints = Enumerable.Repeat(Vector4.zero, KeyPointCount).ToArray();
        public const int KeyPointCount = 21;
        readonly MediaPipe.ResourceSet resourceSet;

        // this detect where the hand is on the image
        readonly MediaPipe.BlazePalm.PalmDetector palm;
        public readonly ComputeBuffer HandRegionBuffer;

        // this detect the hand points
        readonly MediaPipe.HandLandmark.HandLandmarkDetector landmark;
        public readonly ComputeBuffer KeyPointBuffer;
        public readonly ComputeBuffer StatBuffer;
        public Vector4[] Stat;
        public bool Busy;
        public HandTracking(MediaPipe.ResourceSet resources)
        {
            Assert.IsNotNull(resources);
            Busy = false;
            resourceSet = resources;

            palm = new MediaPipe.BlazePalm.PalmDetector(resourceSet.blazePalm);
            landmark = new MediaPipe.HandLandmark.HandLandmarkDetector(resourceSet.handLandmark);

            var regionStructSize = sizeof(float) * 24;
            var filterBufferLength = MediaPipe.HandLandmark.HandLandmarkDetector.VertexCount * 2;

            HandRegionBuffer = new ComputeBuffer(1, regionStructSize);
            KeyPointBuffer = new ComputeBuffer(filterBufferLength, sizeof(float) * 4);
            StatBuffer = new ComputeBuffer(1, 4 * sizeof(float));
            Stat = new[] { Vector4.zero };

            Shader.SetKeyword(GlobalKeyword.Create("NCHW_INPUT"), palm.InputIsNCHW);
        }
        public void Dispose()
        {
            Busy = false;
            palm.Dispose();
            landmark.Dispose();
            HandRegionBuffer.Dispose();
            KeyPointBuffer.Dispose();
            StatBuffer.Dispose();
        }

        public float Score => Stat[0].x;
        public float Handedness => Stat[0].y;

        public void ProcessImage(Texture input)
        {
            if (Busy) return;
            Busy = true;
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
            cs.Dispatch(2, MediaPipe.HandLandmark.HandLandmarkDetector.ImageSize / 8, MediaPipe.HandLandmark.HandLandmarkDetector.ImageSize / 8, 1);

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
                if (!Busy) return;
                req.GetData<Vector4>().CopyTo(HandPoints);
                StatBuffer.GetData(Stat);
                Busy = false;
                BodyPointsUpdatedEvent?.Invoke();
            });
        }
        public event Action BodyPointsUpdatedEvent;

        public Vector3 GetWrist => HandPoints[0];
        public Vector3 GetIndex1 => HandPoints[5];

    }
}
