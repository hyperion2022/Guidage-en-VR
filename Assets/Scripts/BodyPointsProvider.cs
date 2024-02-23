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
    [System.Serializable]
    public struct BodyPoints {
        public Vector4 leftWrist;
        public Vector4 rightWrist;
        public Vector4 leftIndex;
        public Vector4 rightIndex;
        public Vector4 head;

        public static BodyPoints Default = new BodyPoints{
            leftWrist = Vector4.zero,
            rightWrist = Vector4.zero,
            leftIndex = Vector4.zero,
            rightIndex = Vector4.zero,
            head = Vector4.zero,
        };
    }

    public delegate void BodyPointsUpdated(BodyPoints newPoints);
    public event BodyPointsUpdated BodyPointsUpdatedEvent;
    public void EmitBodyPointsUpdatedEvent(BodyPoints bodyPoints) {
        BodyPointsUpdatedEvent?.Invoke(bodyPoints);
    }
}

