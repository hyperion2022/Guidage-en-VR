using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using static BodyPointsProvider;
using System.IO;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System;

public class CalibrationManager : MonoBehaviour
{
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] GameObject detectionManager;
    [SerializeField] GameObject UI;
    [SerializeField] UnityEngine.UI.Button button;
    [Space]
    [SerializeField] GameObject targetTopLeft;
    [SerializeField] GameObject targetTopRight;
    [SerializeField] GameObject targetBottomLeft;
    [SerializeField] GameObject targetBottomRight;
    [SerializeField] GameObject targetCenterLeft;
    [SerializeField] GameObject targetCenterRight;

    private enum Point
    {
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3,
        CenterLeft = 4,
        CenterRight = 5,
    }
    private enum State
    {
        Wait,
        Accu,
        Finished,
    }
    private (State state, Point point) state;

    private Dictionary<Point, (Vector3 head, Vector3 index)> bodyPoints;
    private Vector3[] screenPoints;
    private (int i, Vector3 head, Vector3 index) cumulated;

    private static Point[] points = { Point.TopLeft, Point.TopRight, Point.BottomLeft, Point.BottomRight, Point.CenterLeft, Point.CenterRight };
    private GameObject[] Targets => new[]{
        targetTopLeft,
        targetTopRight,
        targetBottomLeft,
        targetBottomRight,
        targetCenterLeft,
        targetCenterRight,
    };

    void Start()
    {
        foreach (var target in Targets) Assert.IsNotNull(target);

        cumulated = (0, Vector4.zero, Vector4.zero);

        detectionManager.SetActive(false);

        bodyPoints = points.ToDictionary(k => k, _ => (Vector3.zero, Vector3.zero));
        screenPoints = new Vector3[3];

        state = (State.Wait, Point.TopLeft);
        bodyPointsProvider.BodyPointsChanged += Accumulate;
        SetVisualTarget();
    }

    private void Accumulate()
    {
        if (state.state == State.Accu) {
            var head = bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
            var index = bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
            // ignore if not properly tracked
            if (!IsTracked(head)) return;
            if (!IsTracked(index)) return;
            // cumulate it
            cumulated.i += 1;
            cumulated.head += (Vector3)head;
            cumulated.index += (Vector3)index;
            Targets[(int)state.point].transform.localScale = TargetScale() * Vector3.one;
            if (cumulated.i == 10)
            {
                cumulated.head /= 10f;
                cumulated.index /= 10f;

                // 3d visual cues for debug purposes
                new Visual.Sphere(transform, 0.02f, Color.green, $"Head {state.point}") { At = cumulated.head };
                new Visual.Sphere(transform, 0.02f, Color.blue, $"Index {state.point}") { At = cumulated.index };
                new Visual.Cylinder(transform, 0.01f, Color.blue, $"Line {state.point}") { Between = (cumulated.head, cumulated.index) };
                new Visual.Cylinder(transform, 0.005f, Color.yellow, $"Line {state.point}") { Between = (cumulated.head, cumulated.index) };

                bodyPoints[state.point] = (cumulated.head, cumulated.index);
                cumulated = (0, Vector3.zero, Vector3.zero);
                Targets[(int)state.point].SetActive(false);


                state = state.point switch
                {
                    Point.TopLeft => (State.Wait, Point.TopRight),
                    Point.TopRight => (State.Wait, Point.BottomLeft),
                    Point.BottomLeft => (State.Wait, Point.BottomRight),
                    Point.BottomRight => (State.Wait, Point.CenterLeft),
                    Point.CenterLeft => (State.Wait, Point.CenterRight),
                    Point.CenterRight => (State.Finished, Point.TopLeft),
                    _ => throw new InvalidOperationException(),
                };
                switch (state.state)
                {
                    case State.Wait:
                        button.interactable = true;
                        SetVisualTarget();
                        break;
                    case State.Finished:
                        UI.SetActive(false); // d�sactiver l'interface de calibration
                        CornerCoordsFromBodyPoints();
                        var p = screenPoints.Select(v => new[] { v.x, v.y, v.z }).ToArray();
                        var json = JsonConvert.SerializeObject(new Calibration
                        {
                            score = 0.5f,
                            tl = p[(int)Point.TopLeft],
                            tr = p[(int)Point.TopRight],
                            bl = p[(int)Point.BottomLeft],
                        });
                        File.WriteAllText("calibration.json", json);
                        detectionManager.SetActive(true);
                        break;
                }
            }
        }
    }

    private float TargetScale()
    {
        return 2f * (1f - cumulated.i / 9f);
    }

    private void SetVisualTarget()
    {
        Assert.IsTrue(state.state == State.Wait);
        Targets[(int)state.point].transform.localScale = TargetScale() * Vector3.one;
        Targets[(int)state.point].SetActive(true);
    }

    public void Update()
    {
        if (state.state == State.Wait) {
            Targets[(int)state.point].transform.localScale = (TargetScale() + 0.2f * Mathf.Sin(Time.timeSinceLevelLoad * 4f)) * Vector3.one;
        }
        if (Input.GetKeyDown(KeyCode.Space)) UpdateCorner();
    }
    public void UpdateCorner()
    {
        switch (state.state)
        {
            case State.Wait:
                state.state = State.Accu;
                button.interactable = false;
                break;
            default: break;
        }
    }

    [Serializable]
    public struct Calibration
    {
        public float score;
        public float[] tl;
        public float[] tr;
        public float[] bl;

        public static Vector3 ToVector3(float[] point) => new(point[0], point[1], point[2]);
    }

    private void CornerCoordsFromBodyPoints()
    {
        var tl = bodyPoints[Point.TopLeft];
        var tr = bodyPoints[Point.TopRight];
        var bl = bodyPoints[Point.BottomLeft];
        var br = bodyPoints[Point.BottomRight];
        var cl = bodyPoints[Point.CenterLeft];
        var cr = bodyPoints[Point.CenterRight];
        // average head position
        var head = (tl.head + tr.head + bl.head + br.head) / 4f;
        // center point on screen
        var (centered, center1, center2) = EdgeOnEdgeIntersection(cl.head, cr.head, cl.index - cl.head, cr.index - cr.head);
        if (!centered) Debug.Log($"Calibration: Lines are parralel");
        var center = (center1 + center2) / 2f;
        // screen corners where pointing intersect with plane passing through center and perpendicular to head direction
        // (considering the screen as beeing perpendicular is not necessarly covering all use cases, like in multi monitors,
        // where the pointing screen might be on one side and not necessarly properly oriented toward the user)
        var (_, ptl) = EdgeOnPlaneIntersection(tl.head, tl.index - tl.head, center, head - center);
        var (_, ptr) = EdgeOnPlaneIntersection(tr.head, tr.index - tr.head, center, head - center);
        var (_, pbl) = EdgeOnPlaneIntersection(bl.head, bl.index - bl.head, center, head - center);
        var (_, pbr) = EdgeOnPlaneIntersection(br.head, br.index - br.head, center, head - center);// an extra corner that can be used to compute a calibration score
        screenPoints[(int)Point.TopLeft] = ptl;
        screenPoints[(int)Point.TopRight] = ptr;
        screenPoints[(int)Point.BottomLeft] = pbl;

        // visual cues to debug and see what's happening
        new Visual.Sphere(transform, 0.02f, Color.green, "Screen Center") { At = center };
        new Visual.Sphere(transform, 0.02f, Color.white, "Screen Top Left") { At = ptl };
        new Visual.Sphere(transform, 0.02f, Color.white, "Screen Top Right") { At = ptr };
        new Visual.Sphere(transform, 0.02f, Color.white, "Screen Bottom Left") { At = pbl };
        new Visual.Sphere(transform, 0.02f, Color.green, "Screen Bottom Right") { At = pbr };
        new Visual.Sphere(transform, 0.02f, Color.cyan, "Kinect Camera") { At = Vector3.zero };

        new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Top") { Between = (ptl, ptr) };
        new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Right") { Between = (ptr, pbr) };
        new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Bottom") { Between = (pbr, pbl) };
        new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Left") { Between = (pbl, ptl) };
    }

    // For the given line (o + tv) and the given plane (p point on the plane, n the normal vector)
    // returns the point where the two intersect, `false` if the line is perpendicular to the normal
    public static (bool, Vector3) EdgeOnPlaneIntersection(Vector3 o, Vector3 v, Vector3 p, Vector3 n)
    {
        var dot = Vector3.Dot(v, n);
        if (dot == 0f) return (false, Vector3.zero);
        return (true, o + Vector3.Dot(n, p - o) / dot * v);
    }

    // For 2 given lines (o + tv), finds the closest points on those lines.
    // If the two line are parallel, returns `false`.
    private static (bool, Vector3, Vector3) EdgeOnEdgeIntersection(Vector3 o1, Vector3 o2, Vector3 v1, Vector3 v2)
    {
        // It assumes, the shortest segment connecting 2 lines, must be perpendicular to both.
        var c = new Matrix4x4(v1, v2, Vector4.zero, Vector4.zero);
        var a = c.transpose * c;
        // The matrix should be 2x2, but as Unity only provides 4x4,
        // we have to introduce identity values at Z and W, if we want the matrix to be inversible
        a[2, 2] = 1f;
        a[3, 3] = 1f;
        // it's also important to truncate to vector2, because the extra Z and W dimensions introduce undesired values
        var t = a.inverse * (Vector2)(c.transpose * (o1 - o2));
        // now `t` contains [-t1, t2, _, _]
        return (a.determinant != 0f, o1 - t.x * v1, o2 + t.y * v2);
    }
}
