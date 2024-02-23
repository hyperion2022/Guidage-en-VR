using System;
using UnityEngine;

namespace MediaPipe.HandPose {

    public abstract class HandProvider: MonoBehaviour {
        public abstract Vector4[] GetKeyPoints();
        public Vector3 GetKeyPoint(KeyPoint point)
            => GetKeyPoints()[(int)point];

        public Vector3 GetKeyPoint(int index)
            => GetKeyPoints()[index];
        public const int KeyPointCount = 21;
        public enum KeyPoint
        {
            Wrist,
            Thumb1,  Thumb2,  Thumb3,  Thumb4,
            Index1,  Index2,  Index3,  Index4,
            Middle1, Middle2, Middle3, Middle4,
            Ring1,   Ring2,   Ring3,   Ring4,
            Pinky1,  Pinky2,  Pinky3,  Pinky4
        }

        // this is to serialize a list of hand positions to JSON
        // we need 2 struct, because the JSON serializer of C# is too stupid to serialize arrays
        [Serializable]
        public struct Recorded {
            public float rate;
            public Hand[] recs;
        }
        [Serializable]
        public struct Hand {
            public Vector4[] points;
        }
 
    }
}