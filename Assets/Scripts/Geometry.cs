using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

// a namespace (static class) with different geometry related functions
static class Geometry
{
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