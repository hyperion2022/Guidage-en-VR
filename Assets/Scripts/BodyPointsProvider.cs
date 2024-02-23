using System;
using UnityEngine;

// https://github.com/keijiro/HandPoseBarracuda
public abstract class BodyPointsProvider: MonoBehaviour {
    public abstract BodyPoints GetBodyPoints();

    // Each joint is a vector4 with (x, y, z, w)
    // the W component means:
    // - 0 absent (ignore the value)
    // - 1 capted (the value is valid)
    // - 2 guessed
    public class BodyPoints {
        public Vector4 leftWrist = Vector4.zero;
        public Vector4 rightWrist = Vector4.zero;
        public Vector4 leftIndex = Vector4.zero;
        public Vector4 rightIndex = Vector4.zero;
        public Vector4 head = Vector4.zero;
    }
}

