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
    [SerializeField] int cumulate = 10;
    [SerializeField] string filePath = "calibration.json";
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] DetectionManager detectionManager;
    [SerializeField] Canvas UI;
    [SerializeField] UnityEngine.UI.Button button;
    [SerializeField] UnityEngine.UI.Text instructions;
    [Space]
    [SerializeField] GameObject targetTopLeft;
    [SerializeField] GameObject targetTopRight;
    [SerializeField] GameObject targetBottomLeft;
    [SerializeField] GameObject targetBottomRight;

    private enum Point { TopLeft = 0, TopRight = 1, BottomLeft = 2, BottomRight = 3, }
    private enum Lean { Center, Left, Right, }
    private enum State { Init, Wait, Accu, Finished, }
    private (State state, Lean lean, Point point) state;
    private Dictionary<(Point, Lean), (Vector3 head, Vector3 index)> bodyPoints;
    private (int i, Vector3 head, Vector3 index) cumulated;

    // private static Point[] points = { Point.TopLeft, Point.TopRight, Point.BottomLeft, Point.BottomRight };
    private GameObject[] Targets => new[]{
        targetTopLeft,
        targetTopRight,
        targetBottomLeft,
        targetBottomRight,
    };

    static Color ColorFromPoint(Point point) => point switch {
        Point.TopLeft => Color.red,
        Point.TopRight => Color.green,
        Point.BottomLeft => Color.blue,
        Point.BottomRight => Color.magenta,
        _ => throw new InvalidOperationException(),
    };

    void Start()
    {
        Assert.IsNotNull(bodyPointsProvider);
        Assert.IsNotNull(detectionManager);
        Assert.IsNotNull(UI);
        Assert.IsNotNull(button);
        Assert.IsNotNull(instructions);
        foreach (var target in Targets) Assert.IsNotNull(target);
        cumulated = (0, Vector4.zero, Vector4.zero);
        bodyPoints = new();
        state = (State.Init, Lean.Center, Point.TopLeft);
        bodyPointsProvider.BodyPointsChanged += Accumulate;
    }

    private void Accumulate()
    {
        if (state.state == State.Init)
        {
            state = (State.Wait, Lean.Center, Point.TopLeft);
            button.interactable = true;
            SetVisualTarget();
            instructions.text = "Point with your index toward the center of the blue target\nValidate (press space) and stay still until the target disapear";
        }
        else if (state.state == State.Accu)
        {
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
            if (cumulated.i == cumulate)
            {
                cumulated.head /= cumulate;
                cumulated.index /= cumulate;


                // Debug.Log($"Calibration: {state.lean} {state.point}");
                bodyPoints[(state.point, state.lean)] = (cumulated.head, cumulated.index);
                cumulated = (0, Vector3.zero, Vector3.zero);
                Targets[(int)state.point].SetActive(false);


                var (next, point) = state.point switch
                {
                    Point.TopLeft => (State.Wait, Point.TopRight),
                    Point.TopRight => (State.Wait, Point.BottomLeft),
                    Point.BottomLeft => (State.Wait, Point.BottomRight),
                    Point.BottomRight => (State.Finished, Point.TopLeft),
                    _ => throw new InvalidOperationException(),
                };
                if (next == State.Finished && state.lean == Lean.Center)
                {
                    state = (State.Wait, Lean.Left, point);
                }
                else if (next == State.Finished && state.lean == Lean.Left)
                {
                    state = (State.Wait, Lean.Right, point);
                }
                else
                {
                    state = (next, state.lean, point);
                }
                switch (state.state)
                {
                    case State.Wait:
                        button.interactable = true;
                        SetVisualTarget();
                        break;
                    case State.Finished:
                        UI.gameObject.SetActive(false); // d�sactiver l'interface de calibration
                        var c = CalibrationFromBodyPoints();
                        detectionManager.SetCalibration(c);
                        c.SaveToFile(filePath);
                        break;
                }
            }
        }
    }

    private float TargetScale()
    {
        return 1.8f * (1f - cumulated.i / (float)(cumulate - 1));
    }

    private void SetVisualTarget()
    {
        Assert.IsTrue(state.state == State.Wait);
        Targets[(int)state.point].transform.localScale = TargetScale() * Vector3.one;
        Targets[(int)state.point].SetActive(true);
    }

    public void Update()
    {
        if (state.state == State.Wait)
        {
            Targets[(int)state.point].transform.localScale = (TargetScale() + 0.2f * Mathf.Sin(Time.timeSinceLevelLoad * 4f)) * Vector3.one;
        }
        if (button.interactable && Input.GetKeyDown(KeyCode.Space))
        {
            UpdateCorner();
        }
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

    public struct Calibration
    {
        public float score;
        public Vector3 tl;
        public Vector3 x;
        public Vector3 y;

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(new CalibrationJson
            {
                score = score,
                tl = new[] { tl.x, tl.y, tl.z },
                x = new[] { x.x, x.y, x.z },
                y = new[] { y.x, y.y, y.z },
            }));
        }
        public static Calibration LoadFromFile(string filePath)
        {
            var v = JsonConvert.DeserializeObject<CalibrationJson>(File.ReadAllText(filePath));
            if (v.tl != null && v.x != null && v.y != null && v.tl.Length == 3 && v.x.Length == 3 && v.y.Length == 3)
            {
                return new()
                {
                    score = v.score,
                    tl = new(v.tl[0], v.tl[1], v.tl[2]),
                    x = new(v.x[0], v.x[1], v.x[2]),
                    y = new(v.y[0], v.y[1], v.y[2])
                };
            }
            else
            {
                return new()
                {
                    score = 0f,
                    tl = Vector3.zero,
                    x = Vector3.zero,
                    y = Vector3.zero
                };
            }
        }
    }

    [Serializable]
    private struct CalibrationJson
    {
        public float score;
        public float[] tl;
        public float[] x;
        public float[] y;
    }

    private Vector3 TripleIntersect(Point point)
    {
        var (head1, index1) = bodyPoints[(point, Lean.Center)];
        var (head2, index2) = bodyPoints[(point, Lean.Left)];
        var (head3, index3) = bodyPoints[(point, Lean.Right)];

        // 3d visual cues for debug purposes
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point), $"Head {Lean.Center} {point}") { At = head1 };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point), $"Index {Lean.Center} {point}") { At = index1 };
        new Visual.Cylinder(transform, 0.01f, ColorFromPoint(point), $"Line {Lean.Center} {point}") { Between = (head1, index1) };
        new Visual.Cylinder(transform, 0.005f, ColorFromPoint(point), $"Line {Lean.Center} {point}") { Toward = (head1, index1) };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point), $"Head {Lean.Left} {point}") { At = head2 };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point), $"Index {Lean.Left} {point}") { At = index2 };
        new Visual.Cylinder(transform, 0.01f, ColorFromPoint(point), $"Line {Lean.Left} {point}") { Between = (head2, index2) };
        new Visual.Cylinder(transform, 0.005f, ColorFromPoint(point), $"Line {Lean.Left} {point}") { Toward = (head2, index2) };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point), $"Head {Lean.Right} {point}") { At = head3 };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point), $"Index {Lean.Right} {point}") { At = index3 };
        new Visual.Cylinder(transform, 0.01f, ColorFromPoint(point), $"Line {Lean.Right} {point}") { Between = (head3, index3) };
        new Visual.Cylinder(transform, 0.005f, ColorFromPoint(point), $"Line {Lean.Right} {point}") { Toward = (head3, index3) };

        var (_, p1a, p1b) = EdgeOnEdgeIntersection((head1, index1 - head1), (head2, index2 - head2));
        var (_, p2a, p2b) = EdgeOnEdgeIntersection((head2, index2 - head2), (head3, index3 - head3));
        var (_, p3a, p3b) = EdgeOnEdgeIntersection((head3, index3 - head3), (head1, index1 - head1));
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point)) { At = p1a };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point)) { At = p1b };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point)) { At = p2a };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point)) { At = p2b };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point)) { At = p3a };
        new Visual.Sphere(transform, 0.02f, ColorFromPoint(point)) { At = p3b };
        return (p1a + p1b + p2a + p2b + p3a + p3b) / 6f;
    }

    private Calibration CalibrationFromBodyPoints()
    {
        var tl = TripleIntersect(Point.TopLeft);
        var tr = TripleIntersect(Point.TopRight);
        var bl = TripleIntersect(Point.BottomLeft);
        var br = TripleIntersect(Point.BottomRight);

        // new Visual.Sphere(transform, 0.02f, Color.blue, "Top Left") { At = tl };
        // new Visual.Sphere(transform, 0.02f, Color.blue, "Top Right") { At = tr };
        // new Visual.Sphere(transform, 0.02f, Color.blue, "Bottom Left") { At = bl };
        // new Visual.Sphere(transform, 0.02f, Color.blue, "Bottom Right") { At = br };

        var center = (tl + tr + bl + br) / 4f;

        var vl = (tl + bl) / 2f - center;
        var vr = (tr + br) / 2f - center;
        var vt = (tl + tr) / 2f - center;
        var vb = (bl + br) / 2f - center;

        var x = vr - vl;
        var y = vb - vt;
        var w = x.magnitude;
        var h = y.magnitude;

        var normal = Vector3.Cross(y, x).normalized;
        var yc = Vector3.Cross(x, normal);
        var xc = Vector3.Cross(normal, y);
        x = (x + xc).normalized * w;
        y = (y + yc).normalized * h;

        var origin = center - (x + y) / 2f;
        return new() { score = 1f, tl = origin, x = x, y = y, };

        // visual cues to debug and see what's happening
        // new Visual.Sphere(transform, 0.02f, Color.green, "Screen Center") { At = center };
        // new Visual.Sphere(transform, 0.02f, Color.white, "Screen Top Left") { At = ptl };
        // new Visual.Sphere(transform, 0.02f, Color.white, "Screen Top Right") { At = ptr };
        // new Visual.Sphere(transform, 0.02f, Color.white, "Screen Bottom Left") { At = pbl };
        // new Visual.Sphere(transform, 0.02f, Color.green, "Screen Bottom Right") { At = pbr };
        // new Visual.Sphere(transform, 0.02f, Color.cyan, "Kinect Camera") { At = Vector3.zero };

        // new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Top") { Between = (ptl, ptr) };
        // new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Right") { Between = (ptr, pbr) };
        // new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Bottom") { Between = (pbr, pbl) };
        // new Visual.Cylinder(transform, 0.01f, Color.red, $"Screen Left") { Between = (pbl, ptl) };
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
    private static (bool, Vector3, Vector3) EdgeOnEdgeIntersection((Vector3 o, Vector3 v) e1, (Vector3 o, Vector3 v) e2)
    {
        // It assumes, the shortest segment connecting 2 lines, must be perpendicular to both.
        var c = new Matrix4x4(e1.v, e2.v, Vector4.zero, Vector4.zero);
        var a = c.transpose * c;
        // The matrix should be 2x2, but as Unity only provides 4x4,
        // we have to introduce identity values at Z and W, if we want the matrix to be inversible
        a[2, 2] = 1f;
        a[3, 3] = 1f;
        // it's also important to truncate to vector2, because the extra Z and W dimensions introduce undesired values
        var t = a.inverse * (Vector2)(c.transpose * (e1.o - e2.o));
        // now `t` contains [-t1, t2, _, _]
        return (a.determinant != 0f, e1.o - t.x * e1.v, e2.o + t.y * e2.v);
    }
}
