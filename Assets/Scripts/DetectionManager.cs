using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using static BodyPointsProvider;
using static CalibrationManager;

public class DetectionManager : MonoBehaviour
{
    [SerializeField] string filePath = "calibration.json";
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] BodyPointsVisualizer visualizer;

    public (bool valid, Vector2 pos) PointingAt = (false, Vector2.zero);
    private float lastUpdate;

    private (
        Vector3 tl,
        Vector3 x,
        Vector3 y,
        Vector3 n,
        int w,
        int h
    ) screen;

    private (
        Visual.Sphere p,
        Visual.Cylinder l,
        Visual.Cylinder r,
        Visual.Cylinder t,
        Visual.Cylinder b
    ) debug;

    private void Start()
    {
        Assert.IsNotNull(bodyPointsProvider);
        screen.n = Vector3.zero;
        if (visualizer != null)
        {
            debug.l = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue, "Screen Left Side");
            debug.r = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue, "Screen Right Side");
            debug.t = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue, "Screen Top Side");
            debug.b = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue, "Screen Bottom Side");
            debug.p = new(visualizer.transform, 0.03f, Visual.blue, "Pointing at");
        }
        try { LoadFromFile(); }
        catch (FileNotFoundException) { }
        bodyPointsProvider.BodyPointsChanged += UpdatePointer;
    }

    public void SetCalibration(Calibration c)
    {
        screen.tl = c.tl;
        screen.x = c.x;
        screen.y = c.y;
        screen.n = Vector3.Cross(c.x, c.y).normalized;
        screen.w = Screen.width;
        screen.h = Screen.height;
        if (visualizer != null)
        {
            debug.l.Between = (c.tl, c.tl + c.y);
            debug.r.Between = (c.tl + c.x, c.tl + c.y + c.x);
            debug.t.Between = (c.tl, c.tl + c.x);
            debug.b.Between = (c.tl + c.y, c.tl + c.x + c.y);
        }
    }

    public void LoadFromFile(string filePath)
    {
        var c = Calibration.LoadFromFile(filePath);
        if (c.score == 0f) return;
        SetCalibration(c);
    }
    public void LoadFromFile() => LoadFromFile(filePath);

    void Update() {
        if (Time.timeSinceLevelLoad - lastUpdate > 2f) PointingAt.valid = false;
    }

    void UpdatePointer()
    {
        lastUpdate = Time.timeSinceLevelLoad;
        if (screen.x == Vector3.zero) return;
        var head = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
        var index = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
        var (found, point) = LineOnPlaneIntersection(line: (head, (index - head).normalized), plane: (screen.tl, screen.n));
        if (!found) return;
        if (visualizer != null) debug.p.At = point;
        Vector2 pos = new(
            Vector3.Dot(screen.x, point - screen.tl) / screen.x.sqrMagnitude,
            Vector3.Dot(screen.y, point - screen.tl) / screen.y.sqrMagnitude
        );
        PointingAt = (pos.x >= 0.0f && pos.x <= 1.0f && pos.y >= 0.0f && pos.y <= 1.0f, pos);
    }
}
