using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using BodyPoint = BodyPointsProvider.BodyPoint;

public class BodyPointsVisualizer : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;

    private bool updated;
    (BodyPoint, VisualPrimitives.Sphere)[] spheres;
    (BodyPoint, BodyPoint, VisualPrimitives.Cylinder)[] cylinders;

    static readonly Dictionary<(BodyPoint p1, BodyPoint p2), float> bones = new()
    {
        [(BodyPoint.LeftThumb1, BodyPoint.LeftThumb2)] = 0.03f,
        [(BodyPoint.LeftThumb2, BodyPoint.LeftThumb3)] = 0.03f,
        [(BodyPoint.LeftThumb3, BodyPoint.LeftThumb)] = 0.03f,
        [(BodyPoint.LeftIndex1, BodyPoint.LeftIndex2)] = 0.03f,
        [(BodyPoint.LeftIndex2, BodyPoint.LeftIndex3)] = 0.03f,
        [(BodyPoint.LeftIndex3, BodyPoint.LeftIndex)] = 0.03f,
        [(BodyPoint.LeftMiddle1, BodyPoint.LeftMiddle2)] = 0.03f,
        [(BodyPoint.LeftMiddle2, BodyPoint.LeftMiddle3)] = 0.03f,
        [(BodyPoint.LeftMiddle3, BodyPoint.LeftMiddle)] = 0.03f,
        [(BodyPoint.LeftRing1, BodyPoint.LeftRing2)] = 0.03f,
        [(BodyPoint.LeftRing2, BodyPoint.LeftRing3)] = 0.03f,
        [(BodyPoint.LeftRing3, BodyPoint.LeftRing)] = 0.03f,
        [(BodyPoint.LeftPinky1, BodyPoint.LeftPinky2)] = 0.03f,
        [(BodyPoint.LeftPinky2, BodyPoint.LeftPinky3)] = 0.03f,
        [(BodyPoint.LeftPinky3, BodyPoint.LeftPinky)] = 0.03f,
        [(BodyPoint.LeftWrist, BodyPoint.LeftPinky1)] = 0.03f,
        [(BodyPoint.LeftPinky1, BodyPoint.LeftRing1)] = 0.03f,
        [(BodyPoint.LeftRing1, BodyPoint.LeftMiddle1)] = 0.03f,
        [(BodyPoint.LeftMiddle1, BodyPoint.LeftIndex1)] = 0.03f,
        [(BodyPoint.LeftIndex1, BodyPoint.LeftThumb1)] = 0.03f,
        [(BodyPoint.LeftThumb1, BodyPoint.LeftWrist)] = 0.03f,

        [(BodyPoint.RightThumb1, BodyPoint.RightThumb2)] = 0.03f,
        [(BodyPoint.RightThumb2, BodyPoint.RightThumb3)] = 0.03f,
        [(BodyPoint.RightThumb3, BodyPoint.RightThumb)] = 0.03f,
        [(BodyPoint.RightIndex1, BodyPoint.RightIndex2)] = 0.03f,
        [(BodyPoint.RightIndex2, BodyPoint.RightIndex3)] = 0.03f,
        [(BodyPoint.RightIndex3, BodyPoint.RightIndex)] = 0.03f,
        [(BodyPoint.RightMiddle1, BodyPoint.RightMiddle2)] = 0.03f,
        [(BodyPoint.RightMiddle2, BodyPoint.RightMiddle3)] = 0.03f,
        [(BodyPoint.RightMiddle3, BodyPoint.RightMiddle)] = 0.03f,
        [(BodyPoint.RightRing1, BodyPoint.RightRing2)] = 0.03f,
        [(BodyPoint.RightRing2, BodyPoint.RightRing3)] = 0.03f,
        [(BodyPoint.RightRing3, BodyPoint.RightRing)] = 0.03f,
        [(BodyPoint.RightPinky1, BodyPoint.RightPinky2)] = 0.03f,
        [(BodyPoint.RightPinky2, BodyPoint.RightPinky3)] = 0.03f,
        [(BodyPoint.RightPinky3, BodyPoint.RightPinky)] = 0.03f,
        [(BodyPoint.RightWrist, BodyPoint.RightPinky1)] = 0.03f,
        [(BodyPoint.RightPinky1, BodyPoint.RightRing1)] = 0.03f,
        [(BodyPoint.RightRing1, BodyPoint.RightMiddle1)] = 0.03f,
        [(BodyPoint.RightMiddle1, BodyPoint.RightIndex1)] = 0.03f,
        [(BodyPoint.RightIndex1, BodyPoint.RightThumb1)] = 0.03f,
        [(BodyPoint.RightThumb1, BodyPoint.RightWrist)] = 0.03f,

        [(BodyPoint.Head, BodyPoint.Neck)] = 0.07f,
        [(BodyPoint.Neck, BodyPoint.SpineShoulder)] = 0.07f,
        [(BodyPoint.SpineShoulder, BodyPoint.LeftShoulder)] = 0.06f,
        [(BodyPoint.SpineShoulder, BodyPoint.RightShoulder)] = 0.06f,
        [(BodyPoint.LeftShoulder, BodyPoint.LeftElbow)] = 0.06f,
        [(BodyPoint.RightShoulder, BodyPoint.RightElbow)] = 0.06f,
        [(BodyPoint.LeftElbow, BodyPoint.LeftWrist)] = 0.05f,
        [(BodyPoint.RightElbow, BodyPoint.RightWrist)] = 0.05f,
    };
    static readonly Dictionary<BodyPoint, float> nodes = new()
    {
        [BodyPoint.Head] = 0.10f,
        [BodyPoint.Neck] = 0.07f,
        [BodyPoint.SpineShoulder] = 0.07f,
        [BodyPoint.LeftShoulder] = 0.07f,
        [BodyPoint.RightShoulder] = 0.07f,
        [BodyPoint.LeftElbow] = 0.06f,
        [BodyPoint.RightElbow] = 0.06f,
        [BodyPoint.LeftWrist] = 0.05f,
        [BodyPoint.RightWrist] = 0.05f,

        [BodyPoint.LeftThumb] = 0.03f,
        [BodyPoint.LeftThumb3] = 0.03f,
        [BodyPoint.LeftThumb2] = 0.03f,
        [BodyPoint.LeftThumb1] = 0.03f,
        [BodyPoint.LeftIndex] = 0.03f,
        [BodyPoint.LeftIndex3] = 0.03f,
        [BodyPoint.LeftIndex2] = 0.03f,
        [BodyPoint.LeftIndex1] = 0.03f,
        [BodyPoint.LeftMiddle] = 0.03f,
        [BodyPoint.LeftMiddle3] = 0.03f,
        [BodyPoint.LeftMiddle2] = 0.03f,
        [BodyPoint.LeftMiddle1] = 0.03f,
        [BodyPoint.LeftRing] = 0.03f,
        [BodyPoint.LeftRing3] = 0.03f,
        [BodyPoint.LeftRing2] = 0.03f,
        [BodyPoint.LeftRing1] = 0.03f,
        [BodyPoint.LeftPinky] = 0.03f,
        [BodyPoint.LeftPinky3] = 0.03f,
        [BodyPoint.LeftPinky2] = 0.03f,
        [BodyPoint.LeftPinky1] = 0.03f,

        [BodyPoint.RightThumb] = 0.03f,
        [BodyPoint.RightThumb3] = 0.03f,
        [BodyPoint.RightThumb2] = 0.03f,
        [BodyPoint.RightThumb1] = 0.03f,
        [BodyPoint.RightIndex] = 0.03f,
        [BodyPoint.RightIndex3] = 0.03f,
        [BodyPoint.RightIndex2] = 0.03f,
        [BodyPoint.RightIndex1] = 0.03f,
        [BodyPoint.RightMiddle] = 0.03f,
        [BodyPoint.RightMiddle3] = 0.03f,
        [BodyPoint.RightMiddle2] = 0.03f,
        [BodyPoint.RightMiddle1] = 0.03f,
        [BodyPoint.RightRing] = 0.03f,
        [BodyPoint.RightRing3] = 0.03f,
        [BodyPoint.RightRing2] = 0.03f,
        [BodyPoint.RightRing1] = 0.03f,
        [BodyPoint.RightPinky] = 0.03f,
        [BodyPoint.RightPinky3] = 0.03f,
        [BodyPoint.RightPinky2] = 0.03f,
        [BodyPoint.RightPinky1] = 0.03f,
    };

    void Start()
    {
        Assert.IsNotNull(bodyPointsProvider);
        updated = true;
        spheres = bodyPointsProvider.ProvidedPoints.Select(k =>
            (k, new VisualPrimitives.Sphere(transform, nodes.GetValueOrDefault(k, 0.05f), Color.white, k.ToString()))
        ).ToArray();
        var list = new List<(BodyPoint, BodyPoint, VisualPrimitives.Cylinder)>();
        foreach (var (k, v) in bones)
        {
            if (
                bodyPointsProvider.ProvidedPoints.Contains(k.p1) &&
                bodyPointsProvider.ProvidedPoints.Contains(k.p2)
            )
            {
                list.Add((k.p1, k.p2, new(transform, v, Color.white)));
            }
        }
        cylinders = list.ToArray();
        bodyPointsProvider.BodyPointsChanged += () => updated = true;
    }

    private static Color TrackingStateToColor(BodyPointsProvider.PointState state)
    {
        return state switch
        {
            BodyPointsProvider.PointState.Tracked => VisualPrimitives.green,
            BodyPointsProvider.PointState.NotProvided => VisualPrimitives.white,
            BodyPointsProvider.PointState.NotTracked => VisualPrimitives.red,
            BodyPointsProvider.PointState.Inferred => VisualPrimitives.blue,
            _ => throw new InvalidOperationException()
        };
    }

    void Update()
    {
        if (updated)
        {
            updated = false;
            foreach (var (k, go) in spheres)
            {
                var (state, pos) = bodyPointsProvider.GetBodyPoint(k);
                go.At = pos;
                go.Color = TrackingStateToColor(state);
            }
            foreach (var (k1, k2, go) in cylinders)
            {
                var v1 = bodyPointsProvider.GetBodyPoint(k1);
                var v2 = bodyPointsProvider.GetBodyPoint(k2);
                go.Between = (v1.pos, v2.pos);
                go.Color = TrackingStateToColor(v1.state);
            }
        }
    }
}
