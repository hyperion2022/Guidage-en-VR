using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://github.com/keijiro/HandPoseBarracuda
public abstract class BodyPointsProvider: MonoBehaviour {

    // Each joint is a vector4 with (x, y, z, w), the W component means:
    public static bool IsAbsent(Vector4 v) => v.w == 0f;
    public static bool IsTracked(Vector4 v) => v.w == 1f;
    public static bool IsGuessed(Vector4 v) => v.w == 2f;
    public static bool IsInvalid(Vector4 v) => v.w == 3f;

    public abstract Vector4 GetBodyPoint(BodyPoint bodyPoint);
    // list of the available points, not all points have to be available,
    // for instance, the kinect does not give hand points, and baracuda only gives hand points
    public abstract BodyPoint[] AvailablePoints { get; }

    public event Action BodyPointsChanged;
    public void RaiseBodyPointsChanged() => BodyPointsChanged?.Invoke();

    public enum BodyPoint {
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
        LeftMiddle,
        LeftMiddle1,
        LeftMiddle2,
        LeftMiddle3,
        LeftRing,
        LeftRing1,
        LeftRing2,
        LeftRing3,
        LeftPinky,
        LeftPinky1,
        LeftPinky2,
        LeftPinky3,
        RightWrist,
        RightIndex,
        RightIndex1,
        RightIndex2,
        RightIndex3,
        RightThumb,
        RightThumb1,
        RightThumb2,
        RightThumb3,
        RightMiddle,
        RightMiddle1,
        RightMiddle2,
        RightMiddle3,
        RightRing,
        RightRing1,
        RightRing2,
        RightRing3,
        RightPinky,
        RightPinky1,
        RightPinky2,
        RightPinky3,
    }
}