using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://github.com/keijiro/HandPoseBarracuda
public abstract class BodyPointsProvider: MonoBehaviour {

    public enum PointState { NotProvided, Tracked, Inferred, NotTracked }
    public abstract (PointState state, Vector3 pos) GetBodyPoint(BodyPoint bodyPoint);
    // list of the provided points, not all points have to be provided,
    // for instance, the kinect does not give hand points, and barracuda only gives hand points
    // this list is constant, all points that the provider will ever give appear in the list
    public abstract BodyPoint[] ProvidedPoints { get; }
    public event Action BodyPointsChanged;
    public void RaiseBodyPointsChanged() => BodyPointsChanged?.Invoke();
    public enum BodyPoint {
        Head,
        Neck,
        SpineShoulder,
        LeftShoulder,
        RightShoulder,
        LeftElbow,
        RightElbow,
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