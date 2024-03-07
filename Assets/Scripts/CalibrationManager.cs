using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using static BodyPointsProvider;
using System.IO;
using UnityEngine.Assertions;

public class CalibrationManager : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;
    [SerializeField]
    GameObject detectionManager;
    public GameObject[] corners;
    public GameObject center;
    public Text instructionText;
    public GameObject UI;
    private int counter;

    private string message;

    private static readonly int TL = 0;// Top Left
    private static readonly int TR = 1;// Top Right
    private static readonly int BL = 2;// Bottom Left
    private static readonly int BR = 3;// Bottom Right
    private static readonly int CL = 4;// Center Left
    private static readonly int CR = 5;// Center Right
    private (Vector3 head, Vector3 index)[] bodyPoints;
    private Vector3[] screenPoints;
    private (int i, Vector4 head, Vector4 index) cumulated;

    // Start is called before the first frame update
    void Start()
    {
        cumulated = (0, Vector4.zero, Vector4.zero);
        if (corners.Length != 4)
        {
            Debug.Log("Error: Only 4 corners allowed!");
        }

        foreach (var corner in corners)
        {
            corner.GetComponent<Image>().color = Color.red;
        }

        detectionManager.SetActive(false);

        bodyPoints = Enumerable.Repeat((Vector3.zero, Vector3.zero), 6).ToArray();
        screenPoints = new Vector3[3];

        counter = 0;
        message = "0. Stand up so that the Kinect can detect all your body and validate when you are ready.";
        instructionText.text = message;
    }

    public void CumulatePoints()
    {
        // gather new points
        var head = bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
        var index = bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
        // ignore if not properly tracked
        if (!IsTracked(head)) return;
        if (!IsTracked(index)) return;
        // cumulate it
        cumulated.i += 1;
        cumulated.head += head;
        cumulated.index += index;

        // if all points cumulated
        if (cumulated.i == 10)
        {
            // stop accumulation
            bodyPointsProvider.BodyPointsChanged -= CumulatePoints;
            // scale down accumulated value
            cumulated.head /= 10f;
            cumulated.index /= 10f;
            // set point state to TRACKED (w = 1)
            cumulated.head.w = 1f;
            cumulated.index.w = 1f;

            // now update corner
            UpdateCorner();
            // but remember to reset
            cumulated.i = 0;
            cumulated.head = Vector4.zero;
            cumulated.index = Vector4.zero;
        }
    }
    public void UpdateCorner()
    {
        // we save 6 positions = 4 corners + 2 center directions
        if (counter >= 1 && counter <= 6)
        {
            // if point should be cumulated
            if (cumulated.i == 0)
            {
                // start listenning for new body points
                bodyPointsProvider.BodyPointsChanged += CumulatePoints;
                // just quit because we are not ready to update corner
                return;
            }
            // else, the cumulated points are ready to be used

            var points = (head: cumulated.head, index: cumulated.index);
            bodyPoints[counter - 1] = points;

            if (!IsTracked(points.head) || !IsTracked(points.index))
            {
                Debug.Log("Calibration Warning: The body is not properly tracked");
            }
            new Visual.Sphere(transform, 0.02f, Color.green, $"Head {counter}") { At = points.head };
            new Visual.Sphere(transform, 0.02f, Color.blue, $"Index {counter}") { At = points.index };
            new Visual.Cylinder(transform, 0.01f, Color.blue, $"Line {counter}") { Between = (points.head, points.index) };
            new Visual.Cylinder(transform, 0.005f, Color.yellow, $"Line {counter}") { Between = (points.head, points.index) };
        }
        // if we validate one of the 4 corners
        if (counter > 0 && counter <= 4)
        {
            corners[counter - 1].GetComponent<Image>().color = Color.green;
        }
        // the 5th and 6th counters correspond to the center point
        if (counter == 6)
        {
            center.GetComponent<Image>().color = Color.green;
            // // bodyPointsProvider.BodyPointsChanged += () => Debug.Log(Detection().ToString());
        }

        counter++;

        switch (counter)
        {
            // Rajouter le choix de la main dominante, on suppose droitier au d�part
            case 1: message = "1. Point towards the upper-left corner and validate it when you are ready."; break;
            case 2: message = "2. Point towards the upper-right corner and validate it when you are ready."; break;
            case 3: message = "3. Point towards the lower-left corner and validate it when you are ready."; break;
            case 4: message = "4. Point towards the lower-right corner and validate it when you are ready."; break;
            case 5: message = "5. a) Point towards the center from your left side and validate it when you are ready."; break;
            case 6: message = "5. b) Point towards the center from your right side and validate it when you are ready."; break;
            case 7: message = "You have finished the calibration step! Validate again to exit."; break;
            case 8:
                {
                    UI.SetActive(false); // d�sactiver l'interface de calibration
                    CornerCoordsFromBodyPoints(bodyPoints);
                    var p = screenPoints.Select(v => new[] { v.x, v.y, v.z }).ToArray();
                    var json = JsonConvert.SerializeObject(new Calibration{
                        score = 0.5f,
                        tl = p[TL],
                        tr = p[TR],
                        bl = p[BL],
                    });
                    Debug.Log($"Saving {screenPoints[TL]}");
                    File.WriteAllText("calibration.json", json);
                    detectionManager.SetActive(true);
                }
                break;
            default: message = "Error!"; break;
        }

        instructionText.text = message;
    }

    [System.Serializable]
    public struct Calibration {
        public float score;
        public float[] tl;
        public float[] tr;
        public float[] bl;

        public static Vector3 ToVector3(float[] point) => new(point[0], point[1], point[2]);
    }

    private void CornerCoordsFromBodyPoints((Vector3 head, Vector3 index)[] bodyPoints)
    {
        Assert.IsTrue(bodyPoints.Length == 6);
        var tl = bodyPoints[TL];// top left
        var tr = bodyPoints[TR];// top right
        var bl = bodyPoints[BL];// bottom left
        var br = bodyPoints[BR];// bottom right
        var cl = bodyPoints[CL];// center left
        var cr = bodyPoints[CR];// center right
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
        screenPoints[TL] = ptl;
        screenPoints[TR] = ptr;
        screenPoints[BL] = pbl;

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
