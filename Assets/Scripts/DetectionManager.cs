using Newtonsoft.Json;
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
    [SerializeField] GameObject cursor;

    public (bool valid, Vector2 pos) PointingAt = (false, Vector2.zero);

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
            debug.l = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue);
            debug.r = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue);
            debug.t = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue);
            debug.b = new Visual.Cylinder(visualizer.transform, 0.02f, Visual.blue);
            debug.p = new(visualizer.transform, 0.03f, Visual.blue, "Pointing at");
        }
        Debug.Log("Detection Manager: Start");
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
        Debug.Log($"Detection: Screen perpendicularity {Vector3.Dot(c.x, c.y)}");
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

    void UpdatePointer()
    {
        PointingAt = Detection();
        if (PointingAt.valid)
        {
            Debug.Log($"Pointing at {PointingAt.pos}");
            if (cursor != null)
            {
                Vector3 cursorPosition = new Vector3(screen.w * PointingAt.pos.x, screen.h * PointingAt.pos.y, 0);
                cursor.transform.position = cursorPosition; // cam.ScreenToWorldPoint(cursorPosition);

            }
        }
    }

    private (bool, Vector2) Detection()
    {
        if (screen.x == Vector3.zero) return (false, Vector2.zero);
        var head = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
        var index = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
        var (found, point) = LineOnPlaneIntersection(line: (head, (index - head).normalized), plane: (screen.tl, screen.n));
        if (!found) return (false, Vector2.zero);
        if (visualizer != null)
        {
            debug.p.At = point;
        }
        point -= screen.tl;
        // Debug.Log($"Dist to origin {point.magnitude}");
        return (true, new(
            Vector3.Dot(screen.x, point) / screen.x.sqrMagnitude,
            Vector3.Dot(screen.y, point) / screen.y.sqrMagnitude
        ));
    }
    // private Vector2 Detection()
    // {
    //     var head = bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
    //     var rightIndex = bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);

    //     Vector3 pointOnScreen = pointAtZ(head, rightIndex, screenPoints[0].z);
    //     float posX = (pointOnScreen.x - screenPoints[0].x) / (screenPoints[1].x - screenPoints[0].x);
    //     float posY = (pointOnScreen.y - screenPoints[0].y) / (screenPoints[2].y - screenPoints[0].y);

    //     // pos sur l'�cran entre 0 et 1
    //     return new Vector2(posX, posY);
    // }

    // point at depth = z al    
    // private Vector3 pointAtZ(Vector3 p1, Vector3 A, float z)
    // {
    //     Vector3 direction = A - p1;
    //     float t = (z - p1.z) / direction.z;
    //     Vector3 point = p1 + t * direction;
    //     return point;
    // }
}
