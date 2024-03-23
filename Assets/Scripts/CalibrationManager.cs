using System.Linq;
using UnityEngine;
using static BodyPointsProvider;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System;
using static Geometry;

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
    [SerializeField] UnityEngine.UI.Image targetFg;
    [SerializeField] UnityEngine.UI.Image targetBg;
    [SerializeField] RectTransform topLeft;

    private static class Message {
        public const string waintingKinect = "Waiting for body detection from the Kinect...\nMake sure it is properly oriented and try to move your arms.";
        public const string instruction = "Point your index towards the center of the blue target.\nValidate (press the space bar) and stay still until the target disapears.";
        public const string stayLeft = "Stay on the left while aiming at the targets.";
        public const string stayRight = "Stay on the right while aiming at the targets.";
        public const string leanLeft = "Lean to your left.";
        public const string leanRight = "Lean to your right.";
        public const string done = "Done";
        public const string saved = "Calibration saved";
    }
    // private static readonly string pointingMessage = "Point your index towards the center of the blue target.\nValidate (press the space bar) and stay still until the target disapears.";
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
    //   (init, _, _)   waiting Kinect availability
    //   (wait, left, topLeft)   waiting user to click button, one clicked goes to (accu, left, topLeft)
    //   (accu, left, topLeft)   accumulating poitings, once finished goes to (wait, left, topRight)
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

    // for debug purposes, matches a color to screen corners
    static Color ColorFromPoint(Point point) => point switch
    {
        Point.TopLeft => VisualPrimitives.red,
        Point.TopRight => VisualPrimitives.green,
        Point.BottomLeft => VisualPrimitives.yellow,
        Point.BottomRight => VisualPrimitives.magenta,
        _ => throw new InvalidOperationException(),
    };

    void Start()
    {
        Assert.IsNotNull(bodyPointsProvider);
        Assert.IsNotNull(UI);
        Assert.IsNotNull(validationButton);
        Assert.IsNotNull(instructions);
        Assert.IsNotNull(targetFg);
        Assert.IsNotNull(targetBg);

        cumulated = new();
        bodyPoints = new();
        calibration = null;
        try
        {
            calibration = Calibration.LoadFromFile(filePath);
            calibration.Visualize(visualizer.transform);
        }
        catch { }

        instructions.text = Message.waintingKinect;
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
                PlaceOnCanvasFromNormalizedPos(cursor.rectTransform, p);
            }
        }

        if (state.state == State.Init)
        {
            // we can leave the Init state
            state = (State.Wait, Lean.Center, Point.TopLeft);
            validationButton.interactable = true;
            SetVisualTarget();
            instructions.text = Message.instruction;
        }
        else if (state.state == State.Accu)
        {
            var head = bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
            var index = bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
            // ignore if not properly tracked
            if (head.state != PointState.Tracked && head.state != PointState.Inferred) return;
            if (index.state != PointState.Tracked && index.state != PointState.Inferred) return;
            // cumulate it
            cumulated.Add((head.pos, index.pos));

            // this gives a feedback to the user by shrinking the blue target
            targetBg.transform.localScale = TargetScale() * Vector3.one;

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
                targetBg.gameObject.SetActive(false);

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
                            Lean.Left => Message.leanLeft,
                            Lean.Right => Message.leanRight,
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
                        instructions.text = Message.done;
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

    private void PlaceOnCanvasFromNormalizedPos(RectTransform rectTransform, Vector2 pos)
    {
        pos *= 2f;
        pos -= Vector2.one;
        pos.Scale(-topLeft.localPosition);
        rectTransform.localPosition = new(pos.x, pos.y, rectTransform.localPosition.z);
    }

    // shorthand to make the desired blue target visible
    private void SetVisualTarget()
    {
        Assert.IsTrue(state.state == State.Wait);
        targetBg.rectTransform.localScale = TargetScale() * Vector3.one;
        var pos = state.point switch
        {
            Point.TopLeft => new Vector2(0f, 0f),
            Point.TopRight => new Vector2(1f, 0f),
            Point.BottomLeft => new Vector2(0f, 1f),
            Point.BottomRight => new Vector2(1f, 1f),
            _ => throw new InvalidOperationException()
        };
        PlaceOnCanvasFromNormalizedPos(targetBg.rectTransform, pos);
        PlaceOnCanvasFromNormalizedPos(targetFg.rectTransform, pos);
        targetBg.gameObject.SetActive(true);
    }

    public void Update()
    {
        if (state.state == State.Wait)
        {
            // animate the blue target by making its scale slightly oscilating with time
            targetBg.transform.localScale = (TargetScale() + 0.2f * Mathf.Sin(Time.timeSinceLevelLoad * 4f)) * Vector3.one;
        }
        if (validationButton.interactable && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Mouse1)))
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
        instructions.text = Message.saved;
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
                    Lean.Left => Message.stayLeft,
                    Lean.Right => Message.stayRight,
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
                instructions.text = Message.instruction;
                break;
            default: break;
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
            var t = visualizer.transform;
            var c = ColorFromPoint(point);
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Head {Lean.Center} {point}") { At = head1 };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Head {Lean.Left} {point}") { At = head2 };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Head {Lean.Right} {point}") { At = head3 };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Index {Lean.Center} {point}") { At = index1 };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Index {Lean.Left} {point}") { At = index2 };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Index {Lean.Right} {point}") { At = index3 };
            new VisualPrimitives.Cylinder(t, 0.01f, c, $"Line {Lean.Center} {point}") { Between = (head1, index1) };
            new VisualPrimitives.Cylinder(t, 0.01f, c, $"Line {Lean.Left} {point}") { Between = (head2, index2) };
            new VisualPrimitives.Cylinder(t, 0.01f, c, $"Line {Lean.Right} {point}") { Between = (head3, index3) };
            new VisualPrimitives.Cylinder(t, 0.005f, c, $"Line {Lean.Center} {point}") { Toward = (head1, index1) };
            new VisualPrimitives.Cylinder(t, 0.005f, c, $"Line {Lean.Left} {point}") { Toward = (head2, index2) };
            new VisualPrimitives.Cylinder(t, 0.005f, c, $"Line {Lean.Right} {point}") { Toward = (head3, index3) };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Estimation {point}") { At = p1a };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Estimation {point}") { At = p1b };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Estimation {point}") { At = p2a };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Estimation {point}") { At = p2b };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Estimation {point}") { At = p3a };
            new VisualPrimitives.Sphere(t, 0.02f, c, $"Estimation {point}") { At = p3b };
            new VisualPrimitives.Sphere(t, 0.03f, c, $"Estimation {point}") { At = median };
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

}
