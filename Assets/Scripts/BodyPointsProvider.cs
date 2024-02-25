using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://github.com/keijiro/HandPoseBarracuda
public abstract class BodyPointsProvider: MonoBehaviour {

    // Each joint is a vector4 with (x, y, z, w)
    // the W component means:
    // - 0 absent (ignore the value) GREY
    // - 1 capted (the value is valid) GREEN
    // - 2 guessed BLUE
    // - 3 error (the value could not be computed) RED
    public static bool IsAbsent(Vector4 v) => v.w == 0f;
    public static bool IsTracked(Vector4 v) => v.w == 1f;
    public static bool IsGuessed(Vector4 v) => v.w == 2f;
    public static bool IsInvalid(Vector4 v) => v.w == 3f;

    public abstract Vector4 GetBodyPoint(Key key);
    public abstract Key[] AvailablePoints { get; }

    public enum Key {
        Head,
        LeftWrist,
        LeftIndex,
        LeftIndex1,
        LeftIndex2,
        LeftIndex3,
        LeftThumb,
        LeftThumb1,
        LeftThumb2,
        LeftThumb3,
        RightWrist,
        RightIndex,
        RightIndex1,
        RightIndex2,
        RightIndex3,
        RightThumb,
        RightThumb1,
        RightThumb2,
        RightThumb3,
    }
    public delegate void BodyPointsUpdated();
    public event BodyPointsUpdated BodyPointsUpdatedEvent;
    public void EmitBodyPointsUpdatedEvent() {
        BodyPointsUpdatedEvent?.Invoke();
    }

    public static string KeyToJson(Key key) => "\"" + key.ToString() + "\"";
}

