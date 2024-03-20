using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using static BodyPointsProvider;
using static Geometry;
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
        VisualPrimitives.Sphere p,
        VisualPrimitives.Cylinder l,
        VisualPrimitives.Cylinder r,
        VisualPrimitives.Cylinder t,
        VisualPrimitives.Cylinder b
    ) visualize;


    public (bool valid, Vector2 pos) PointingAt(BodyPointsProvider bodyPointsProvider)
    {
        if (x == Vector3.zero) return (false, Vector2.zero);
        var head = bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
        var index = bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
        if (head.state != PointState.Tracked) return (false, Vector2.zero);
        if (index.state != PointState.Tracked) return (false, Vector2.zero);
        var (found, point) = LineOnPlaneIntersection(
            line: (head.pos, (index.pos - head.pos).normalized),
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
            p: new(parent, 0.03f, VisualPrimitives.blue, "Pointing At") { At = tl },
            l: new VisualPrimitives.Cylinder(parent, 0.02f, VisualPrimitives.blue, "Screen Left Side") { Between = (tl, tl + y) },
            r: new VisualPrimitives.Cylinder(parent, 0.02f, VisualPrimitives.blue, "Screen Right Side") { Between = (tl + x, tl + y + x) },
            t: new VisualPrimitives.Cylinder(parent, 0.02f, VisualPrimitives.blue, "Screen Top Side") { Between = (tl, tl + x) },
            b: new VisualPrimitives.Cylinder(parent, 0.02f, VisualPrimitives.blue, "Screen Bottom Side") { Between = (tl + y, tl + x + y) }
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
