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
    [SerializeField] int cumulate = 20;
    [SerializeField] string filePath = "calibration.json";
    [SerializeField] BodyPointsProvider bodyPointsProvider;
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
    // there are 4 corners on the screen
    private enum Point { TopLeft = 0, TopRight = 1, BottomLeft = 2, BottomRight = 3, }
    // the user may positionate itself in 3 posture, straight, leaning left, leaning right
    private enum Lean { Center, Left, Right, }
    private enum State
    {
        Init,// waiting for Kinect to go online and start providing body points
        Wait,// waiting for user to click button for pointing capture
        Accu,// once the user ckicked the button, we capture N pointing (to average them to reduce uncertainty)
        Lean,// we ask the user to lean on one of its side, waiting for him to click the button
        Finished,// the calibration step is complete, we offer the user to either save the calibration or restart a new one
    }
    // the full state of the scene would better be described with algebraic data type, but as this is not supported
    // in C#, we just use a tuple. For instance:
    // (init, _, _) waiting Kinect availability
    // (wait, left, topLeft) waiting user to click button, one clicked goes to (accu, left, topLeft)
    // (accu, left, topLeft) accumulating poitings, once finished goes to (wait, left, topRight)
    private (State state, Lean lean, Point point) state;
    // we store the captured pointing, in tatal, there are Point * Lean (4 * 3 = 12) pointings
    // a pointing is both the head and index position (we draw a line passing through both)
    private Dictionary<(Point, Lean), (Vector3 head, Vector3 index)> bodyPoints;
    // when capturing a pointing, we accumulate several captures, then we average them (this reduces incertainty and noise)
    private List<(Vector3 head, Vector3 index)> cumulated;
    // just a way to position things on the interface with normalized coordinates
    // - tl = top left corner position
    // - x = the horizontal unit vector
    // - y = the vertical unit vector
    private Calibration calibration;

    // a convenient way not to repeat code afterwards
    private UnityEngine.UI.Image[] Targets => new[]{
        targetTopLeft,
        targetTopRight,
        targetBottomLeft,
        targetBottomRight,
    };

    // for debug purposes, matches a color to screen corners
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
        Assert.IsNotNull(UI);
        Assert.IsNotNull(validationButton);
        Assert.IsNotNull(instructions);
        foreach (var target in Targets) Assert.IsNotNull(target);

        cumulated = new();
        bodyPoints = new();
        calibration = null;
        try
        {
            calibration = Calibration.LoadFromFile(filePath);
            calibration.Visualize(visualizer.transform);
        }
        catch { }

        state = (State.Init, Lean.Center, Point.TopLeft);
        bodyPointsProvider.BodyPointsChanged += OnBodyPointsChange;
    }

    // this function is called every time the kinect produces a new captation
    private void OnBodyPointsChange()
    {
        // shows a cursor where the user is pointing with its finger
        if (calibration != null)
        {
            var (valid, p) = calibration.PointingAt(bodyPointsProvider);
            if (valid)
            {
                p = p * 2f - Vector2.one;
                cursor.rectTransform.position = new(p.x * 100f, p.y * -100f, cursor.rectTransform.position.z);
            }
        }

        if (state.state == State.Init)
        {
            // we can leave the Init state
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
            cumulated.Add(((Vector3)head, (Vector3)index));

            // this gives a feedback to the user by shrinking the blue target
            Targets[(int)state.point].transform.localScale = TargetScale() * Vector3.one;

            // if we reach the accumulation number
            if (cumulated.Count == cumulate)
            {
                // the average is calculated, except it is not the center of mass, but the geometric median
                // by the geometric median is more resilient to rogue points (the one diverging too much)
                var cumulatedHead = GeometricMedian(cumulated.Select(c => c.head).ToArray());
                var cumulatedIndex = GeometricMedian(cumulated.Select(c => c.index).ToArray());
                cumulated.Clear();

                // storing the pointing
                bodyPoints[(state.point, state.lean)] = (cumulatedHead, cumulatedIndex);
                // hide the blue target
                Targets[(int)state.point].gameObject.SetActive(false);

                // what will be the next state?
                var (next, point) = state.point switch
                {
                    Point.TopLeft => (State.Wait, Point.TopRight),
                    Point.TopRight => (State.Wait, Point.BottomLeft),
                    Point.BottomLeft => (State.Wait, Point.BottomRight),
                    Point.BottomRight => (State.Finished, Point.TopLeft),
                    _ => throw new InvalidOperationException(),
                };
                // if all points done, but not all lean done, do next lean
                if (next == State.Finished && state.lean == Lean.Center)
                {
                    state = (State.Lean, Lean.Left, point);
                }
                // if all points done, but not all lean done, do next lean
                else if (next == State.Finished && state.lean == Lean.Left)
                {
                    state = (State.Lean, Lean.Right, point);
                }
                else
                {
                    state = (next, state.lean, point);
                }
                // now do transition
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
                        if (calibration != null) calibration.VisualizeRemove();
                        calibration = CalibrationFromBodyPoints();
                        calibration.Visualize(visualizer.transform);
                        break;
                }
            }
        }
    }

    // when the accumulation is progressing, the targets shrinks, the following
    // function returns the desired scale (shrinking) for the current accumulation
    private float TargetScale() => 1.8f * (1f - cumulated.Count / (float)(cumulate - 1));

    // shorthand to make the desired blue target visible
    private void SetVisualTarget()
    {
        Assert.IsTrue(state.state == State.Wait);
        Targets[(int)state.point].rectTransform.localScale = TargetScale() * Vector3.one;
        Targets[(int)state.point].gameObject.SetActive(true);
    }

    public void Update()
    {
        if (state.state == State.Wait)
        {
            // animate the blue target by making its scale slightly oscilating with time
            Targets[(int)state.point].transform.localScale = (TargetScale() + 0.2f * Mathf.Sin(Time.timeSinceLevelLoad * 4f)) * Vector3.one;
        }
        if (validationButton.interactable && Input.GetKeyDown(KeyCode.Space))
        {
            // pressing space does the same thing as clicking the button
            OnValidation();
        }
    }

    // serialize to json and save to disk the current calibration
    public void SaveToFile()
    {
        Assert.IsTrue(calibration.x != Vector3.zero);
        calibration.SaveToFile(filePath);
        instructions.text = "Calibration saved";
        saveButton.interactable = false;
    }

    public void OnValidation()
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
                // the user wants to restart the calibration
                // then reset everything to initial state
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

    public class Calibration
    {
        // position of the top left corner
        public Vector3 tl;
        // horizontal vector, with its magnitude beeing the screen's width
        public Vector3 x;
        // vertical vector, with its magnitude beeing the screen's height
        public Vector3 y;

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(new Json
            {
                tl = new[] { tl.x, tl.y, tl.z },
                x = new[] { x.x, x.y, x.z },
                y = new[] { y.x, y.y, y.z },
            }));
        }
        public static Calibration LoadFromFile(string filePath)
        {
            var v = JsonConvert.DeserializeObject<Json>(File.ReadAllText(filePath));
            if (v.tl != null && v.x != null && v.y != null && v.tl.Length == 3 && v.x.Length == 3 && v.y.Length == 3)
            {
                return new()
                {
                    tl = new(v.tl[0], v.tl[1], v.tl[2]),
                    x = new(v.x[0], v.x[1], v.x[2]),
                    y = new(v.y[0], v.y[1], v.y[2])
                };
            }
            else
            {
                return new()
                {
                    tl = Vector3.zero,
                    x = Vector3.zero,
                    y = Vector3.zero
                };
            }
        }
        // just the equivalent of Calibration, but serializable to json
        [Serializable]
        private struct Json { public float[] tl; public float[] x; public float[] y; }

        private (
            Visual.Sphere p,
            Visual.Cylinder l,
            Visual.Cylinder r,
            Visual.Cylinder t,
            Visual.Cylinder b
        ) visualize;


        public (bool valid, Vector2 pos) PointingAt(BodyPointsProvider bodyPointsProvider)
        {
            if (x == Vector3.zero) return (false, Vector2.zero);
            var head = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
            var index = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
            var (found, point) = LineOnPlaneIntersection(
                line: (head, (index - head).normalized),
                plane: (tl, Vector3.Cross(x, y).normalized)
            );
            if (!found) return (false, Vector2.zero);
            if (visualize.p != null) visualize.p.At = point;
            Vector2 pos = new(
                Vector3.Dot(x, point - tl) / x.sqrMagnitude,
                Vector3.Dot(y, point - tl) / y.sqrMagnitude
            );
            return (pos.x >= 0.0f && pos.x <= 1.0f && pos.y >= 0.0f && pos.y <= 1.0f, pos);
        }

        public void Visualize(Transform parent)
        {
            visualize = (
                p: new(parent, 0.03f, Visual.blue, "Pointing At") { At = tl },
                l: new Visual.Cylinder(parent, 0.02f, Visual.blue, "Screen Left Side") { Between = (tl, tl + y) },
                r: new Visual.Cylinder(parent, 0.02f, Visual.blue, "Screen Right Side") { Between = (tl + x, tl + y + x) },
                t: new Visual.Cylinder(parent, 0.02f, Visual.blue, "Screen Top Side") { Between = (tl, tl + x) },
                b: new Visual.Cylinder(parent, 0.02f, Visual.blue, "Screen Bottom Side") { Between = (tl + y, tl + x + y) }
            );
        }
        public void VisualizeRemove()
        {
            if (visualize.p != null)
            {
                visualize.p.Remove();
                visualize.p = null;
                visualize.l.Remove();
                visualize.l = null;
                visualize.r.Remove();
                visualize.r = null;
                visualize.t.Remove();
                visualize.t = null;
                visualize.b.Remove();
                visualize.b = null;
            }
        }
    }


    // for a given screen corner, returns its estimated position by
    // intersecting the 3 pointings from the different lean (center, left, right)
    private Vector3 TripleIntersect(Point point)
    {
        var (head1, index1) = bodyPoints[(point, Lean.Center)];
        var (head2, index2) = bodyPoints[(point, Lean.Left)];
        var (head3, index3) = bodyPoints[(point, Lean.Right)];

        var (_, p1a, p1b) = LineOnLineIntersection((head1, index1 - head1), (head2, index2 - head2));
        var (_, p2a, p2b) = LineOnLineIntersection((head2, index2 - head2), (head3, index3 - head3));
        var (_, p3a, p3b) = LineOnLineIntersection((head3, index3 - head3), (head1, index1 - head1));

        var median = GeometricMedian(new[] { p1a, p1b, p2a, p2b, p3a, p3b });

        // 3d visual cues for debug purposes
        // the `visualizer` acts as a parent for debug GameObject
        if (visualizer != null)
        {
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

    // once all pointings have been captured, we deduce screen position from from them
    private Calibration CalibrationFromBodyPoints()
    {
        // compute the estimated position of the 4 screen corners
        var tl = TripleIntersect(Point.TopLeft);
        var tr = TripleIntersect(Point.TopRight);
        var bl = TripleIntersect(Point.BottomLeft);
        var br = TripleIntersect(Point.BottomRight);
        // this is not enough, as the data is quite noisy,
        // we estimate a better position with crossing redundant informations

        var center = (tl + tr + bl + br) / 4f;
        var vl = (tl + bl) / 2f - center;// left side vector
        var vr = (tr + br) / 2f - center;// right side vector
        var vt = (tl + tr) / 2f - center;// top side vector
        var vb = (bl + br) / 2f - center;// bottom side vector

        var x = vr - vl;// estimated horizontal unit vector
        var y = vb - vt;// estimated vertical unit vector
        var w = x.magnitude;// width
        var h = y.magnitude;// height

        // now, `x` and `y`, due to captation noise and error, are probably not orthogonal
        // we could correct it by moving either `x` or `y`, but the best is to do both and to peek in the middle
        var normal = Vector3.Cross(y, x).normalized;
        var yc = Vector3.Cross(x, normal);
        var xc = Vector3.Cross(normal, y);
        x = (x + xc).normalized * w;
        y = (y + yc).normalized * h;
        // they are not exactly orthogonal, but it doesn't matter

        var origin = center - (x + y) / 2f;
        return new() { tl = origin, x = x, y = y, };
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
