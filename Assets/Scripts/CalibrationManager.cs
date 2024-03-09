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
    [SerializeField] UnityEngine.UI.Button validationButton;
    [SerializeField] UnityEngine.UI.Button restartButton;
    [SerializeField] UnityEngine.UI.Button saveButton;
    [SerializeField] UnityEngine.UI.Text instructions;
    [SerializeField] UnityEngine.UI.Image cursor;
    [SerializeField] BodyPointsVisualizer visualizer;
    [Space]
    [SerializeField] UnityEngine.UI.Image targetTopLeft;
    [SerializeField] UnityEngine.UI.Image targetTopRight;
    [SerializeField] UnityEngine.UI.Image targetBottomLeft;
    [SerializeField] UnityEngine.UI.Image targetBottomRight;

    private static readonly string pointingMessage = "Point with your index toward the center of the blue target\nValidate (press space) and stay still until the target disapear";
    private enum Point { TopLeft = 0, TopRight = 1, BottomLeft = 2, BottomRight = 3, }
    private enum Lean { Center, Left, Right, }
    private enum State { Init, Wait, Accu, Lean, Finished, }
    private (State state, Lean lean, Point point) state;
    private Dictionary<(Point, Lean), (Vector3 head, Vector3 index)> bodyPoints;
    private (int i, Vector3 head, Vector3 index) cumulated;
    private (Vector3 tl, Vector3 x, Vector3 y) screen;
    private Calibration calibration;

    // private static Point[] points = { Point.TopLeft, Point.TopRight, Point.BottomLeft, Point.BottomRight };
    private UnityEngine.UI.Image[] Targets => new[]{
        targetTopLeft,
        targetTopRight,
        targetBottomLeft,
        targetBottomRight,
    };

    static Color ColorFromPoint(Point point) => point switch
    {
        Point.TopLeft => Visual.red,
        Point.TopRight => Visual.green,
        Point.BottomLeft => Visual.yellow,
        Point.BottomRight => Visual.magenta,
        _ => throw new InvalidOperationException(),
    };

    void Start()
    {
        Assert.IsNotNull(bodyPointsProvider);
        Assert.IsNotNull(detectionManager);
        Assert.IsNotNull(UI);
        Assert.IsNotNull(validationButton);
        Assert.IsNotNull(instructions);
        foreach (var target in Targets) Assert.IsNotNull(target);
        screen.tl = targetTopLeft.rectTransform.position;
        screen.x = targetTopRight.rectTransform.position - screen.tl;
        screen.y = targetBottomLeft.rectTransform.position - screen.tl;
        cumulated = (0, Vector4.zero, Vector4.zero);
        bodyPoints = new();
        state = (State.Init, Lean.Center, Point.TopLeft);
        bodyPointsProvider.BodyPointsChanged += OnBodyPointsChange;
    }

    private void OnBodyPointsChange()
    {
        if (state.state == State.Init)
        {
            state = (State.Wait, Lean.Center, Point.TopLeft);
            validationButton.interactable = true;
            SetVisualTarget();
            instructions.text = pointingMessage;
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
                Targets[(int)state.point].gameObject.SetActive(false);


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
                    state = (State.Lean, Lean.Left, point);
                }
                else if (next == State.Finished && state.lean == Lean.Left)
                {
                    state = (State.Lean, Lean.Right, point);
                }
                else
                {
                    state = (next, state.lean, point);
                }
                switch (state.state)
                {
                    case State.Lean:
                        validationButton.interactable = true;
                        instructions.text = state.lean switch
                        {
                            Lean.Left => "Lean on your left",
                            Lean.Right => "Lean on your right",
                            _ => throw new InvalidOperationException(),
                        };
                        break;
                    case State.Wait:
                        validationButton.interactable = true;
                        SetVisualTarget();
                        break;
                    case State.Finished:
                        validationButton.gameObject.SetActive(false);
                        restartButton.gameObject.SetActive(true);
                        saveButton.gameObject.SetActive(true);
                        saveButton.interactable = true;
                        instructions.text = "Done";
                        cursor.gameObject.SetActive(true);
                        // UI.gameObject.SetActive(false); // d�sactiver l'interface de calibration
                        calibration = CalibrationFromBodyPoints();
                        detectionManager.SetCalibration(calibration);
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
        Targets[(int)state.point].rectTransform.localScale = TargetScale() * Vector3.one;
        Targets[(int)state.point].gameObject.SetActive(true);
    }

    public void Update()
    {
        if (state.state == State.Finished)
        {
            var (valid, p) = detectionManager.PointingAt;
            if (valid)
            {
                cursor.rectTransform.position = screen.tl + p.x * screen.x + p.y * screen.y;
            }
        }
        else if (state.state == State.Wait)
        {
            Targets[(int)state.point].transform.localScale = (TargetScale() + 0.2f * Mathf.Sin(Time.timeSinceLevelLoad * 4f)) * Vector3.one;
        }
        if (validationButton.interactable && Input.GetKeyDown(KeyCode.Space))
        {
            UpdateCorner();
        }
    }

    public void SaveToFile() {
        calibration.SaveToFile(filePath);
        instructions.text = "Calibration saved";
        saveButton.interactable = false;
    }

    public void UpdateCorner()
    {
        switch (state.state)
        {
            case State.Wait:
                state.state = State.Accu;
                validationButton.interactable = false;
                break;
            case State.Lean:
                state.state = State.Wait;
                instructions.text = state.lean switch
                {
                    Lean.Left => "Stay on the left while aiming the targets",
                    Lean.Right => "Stay on the right while aiming the targets",
                    _ => throw new InvalidOperationException(),
                };
                SetVisualTarget();
                break;
            case State.Finished:
                cursor.gameObject.SetActive(false);
                state = (State.Wait, Lean.Center, Point.TopLeft);
                restartButton.gameObject.SetActive(false);
                saveButton.gameObject.SetActive(false);
                validationButton.gameObject.SetActive(true);
                validationButton.interactable = true;
                SetVisualTarget();
                instructions.text = pointingMessage;
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


        var (_, p1a, p1b) = LineOnLineIntersection((head1, index1 - head1), (head2, index2 - head2));
        var (_, p2a, p2b) = LineOnLineIntersection((head2, index2 - head2), (head3, index3 - head3));
        var (_, p3a, p3b) = LineOnLineIntersection((head3, index3 - head3), (head1, index1 - head1));

        var median = GeometricMedian(new[] { p1a, p1b, p2a, p2b, p3a, p3b });

        if (visualizer != null)
        {
            // 3d visual cues for debug purposes
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point), $"Head {Lean.Center} {point}") { At = head1 };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point), $"Head {Lean.Left} {point}") { At = head2 };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point), $"Head {Lean.Right} {point}") { At = head3 };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point), $"Index {Lean.Center} {point}") { At = index1 };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point), $"Index {Lean.Left} {point}") { At = index2 };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point), $"Index {Lean.Right} {point}") { At = index3 };
            new Visual.Cylinder(visualizer.transform, 0.01f, ColorFromPoint(point), $"Line {Lean.Center} {point}") { Between = (head1, index1) };
            new Visual.Cylinder(visualizer.transform, 0.01f, ColorFromPoint(point), $"Line {Lean.Left} {point}") { Between = (head2, index2) };
            new Visual.Cylinder(visualizer.transform, 0.01f, ColorFromPoint(point), $"Line {Lean.Right} {point}") { Between = (head3, index3) };
            new Visual.Cylinder(visualizer.transform, 0.005f, ColorFromPoint(point), $"Line {Lean.Center} {point}") { Toward = (head1, index1) };
            new Visual.Cylinder(visualizer.transform, 0.005f, ColorFromPoint(point), $"Line {Lean.Left} {point}") { Toward = (head2, index2) };
            new Visual.Cylinder(visualizer.transform, 0.005f, ColorFromPoint(point), $"Line {Lean.Right} {point}") { Toward = (head3, index3) };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point)) { At = p1a };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point)) { At = p1b };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point)) { At = p2a };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point)) { At = p2b };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point)) { At = p3a };
            new Visual.Sphere(visualizer.transform, 0.02f, ColorFromPoint(point)) { At = p3b };
            new Visual.Sphere(visualizer.transform, 0.03f, ColorFromPoint(point)) { At = median };
        }

        return median;
    }

    private Calibration CalibrationFromBodyPoints()
    {
        var tl = TripleIntersect(Point.TopLeft);
        var tr = TripleIntersect(Point.TopRight);
        var bl = TripleIntersect(Point.BottomLeft);
        var br = TripleIntersect(Point.BottomRight);

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
    }

    // For the given line (o + tv) and the given plane (p point on the plane, n the normal vector)
    // returns the point where the two intersect, `false` if the line is perpendicular to the normal
    public static (bool, Vector3) LineOnPlaneIntersection((Vector3 o, Vector3 v) line, (Vector3 p, Vector3 n) plane)
    {
        var dot = Vector3.Dot(line.v, plane.n);
        if (dot == 0f) return (false, Vector3.zero);
        return (true, line.o + Vector3.Dot(plane.n, plane.p - line.o) / dot * line.v);
    }

    // For 2 given lines (o + tv), finds the closest points on those lines.
    // If the two line are parallel, returns `false`.
    public static (bool, Vector3, Vector3) LineOnLineIntersection((Vector3 o, Vector3 v) line1, (Vector3 o, Vector3 v) line2)
    {
        // It assumes, the shortest segment connecting 2 lines, must be perpendicular to both.
        var c = new Matrix4x4(line1.v, line2.v, Vector4.zero, Vector4.zero);
        var a = c.transpose * c;
        // The matrix should be 2x2, but as Unity only provides 4x4,
        // we have to introduce identity values at Z and W, if we want the matrix to be inversible
        a[2, 2] = 1f;
        a[3, 3] = 1f;
        // it's also important to truncate to vector2, because the extra Z and W dimensions introduce undesired values
        var t = a.inverse * (Vector2)(c.transpose * (line1.o - line2.o));
        // now `t` contains [-t1, t2, _, _]
        return (a.determinant != 0f, line1.o - t.x * line1.v, line2.o + t.y * line2.v);
    }

    // https://en.wikipedia.org/wiki/Geometric_median
    public static Vector3 GeometricMedian(Vector3[] points, int iter = 5)
    {
        Assert.IsTrue(points.Length > 0);
        Assert.IsTrue(iter >= 0);
        Vector3 center = points.Aggregate((a, b) => a + b) / points.Length;
        for (int i = 0; i < iter; i++)
        {
            var a = Vector3.zero;
            var b = 0f;
            foreach (var point in points)
            {
                var d = Vector3.Distance(center, point);
                a += point / d;
                b += 1 / d;
            }
            center = a / b;
        }
        return center;
    }
}
