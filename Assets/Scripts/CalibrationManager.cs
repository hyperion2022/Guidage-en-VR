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
        Debug.Log($"Cumulated: at {cumulated.i}");
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

            Debug.Log($"Cumulated: distance {Vector3.Distance(cumulated.head, cumulated.index)}");
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
            GameObject go;
            go = DebugVisuals.CreateSphere(transform, 0.02f, Color.green, $"Head {counter}");
            DebugVisuals.SphereAt(go, points.head);
            go = DebugVisuals.CreateSphere(transform, 0.02f, Color.blue, $"Index {counter}");
            DebugVisuals.SphereAt(go, points.index);
            go = DebugVisuals.CreateCylinder(transform, 0.01f, Color.blue, $"Line {counter}");
            DebugVisuals.CylinderBetween(go, points.head, points.index);
            go = DebugVisuals.CreateCylinder(transform, 0.005f, Color.yellow, $"Line {counter}");
            DebugVisuals.CylinderToward(go, points.head, points.index);
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
                    File.WriteAllText("screenCorners.json", JsonConvert.SerializeObject(
                        screenPoints.Select(v => new float[] { v.x, v.y, v.z }).ToArray())
                    );
                    detectionManager.SetActive(true);
                }
                break;
            default: message = "Error!"; break;
        }

        instructionText.text = message;
    }

    private void CornerCoordsFromBodyPoints((Vector3 head, Vector3 index)[] bodyPoints)
    {
        if (bodyPoints.Length != 6)
        {
            Debug.Log("bodyPoints array has to be of length 6!");
        }

        // prendre en compte la 4e valeur
        // factoriser les points choisis
        Vector4 dirLeftCenter = bodyPoints[4].index - bodyPoints[4].head;
        Vector4 dirRightCenter = bodyPoints[5].index - bodyPoints[5].head;
        Vector3 p1 = new Vector3(bodyPoints[4].head.x, bodyPoints[4].head.y, bodyPoints[4].head.z);
        Vector3 p2 = new Vector3(bodyPoints[5].head.x, bodyPoints[5].head.y, bodyPoints[5].head.z);
        Vector3 A = new Vector3(dirLeftCenter.x, dirLeftCenter.y, dirLeftCenter.z);
        Vector3 B = new Vector3(dirRightCenter.x, dirRightCenter.y, dirRightCenter.z);
        Vector3 centerPoint = intersectionPoint(p1, p2, A, B);

        // 3 points forment un plan/rectangle, le 4e peut sortir de ce plan � cause d'impr�cisions de calcul
        Vector3 cornerUL = pointAtZ(bodyPoints[0].head, bodyPoints[0].index, centerPoint.z);
        Vector3 cornerUR = pointAtZ(bodyPoints[1].head, bodyPoints[1].index, centerPoint.z);
        Vector3 cornerLL = pointAtZ(bodyPoints[2].head, bodyPoints[2].index, centerPoint.z);
        Vector3 cornerLR = cornerUL + (cornerUR - cornerUL) + (cornerLL - cornerUL);

        // sortie: rectangle (3 points)
        screenPoints[0] = cornerUL;
        screenPoints[1] = cornerUR;
        screenPoints[2] = cornerLL;

        Debug.Log(screenPoints[0]);
        Debug.Log(screenPoints[1]);
        Debug.Log(screenPoints[2]);

        GameObject go;
        go = DebugVisuals.CreateSphere(transform, 0.02f, Color.green, "Screen UL");
        DebugVisuals.SphereAt(go, centerPoint);
        go = DebugVisuals.CreateSphere(transform, 0.02f, Color.white, "Screen UL");
        DebugVisuals.SphereAt(go, cornerUL);
        go = DebugVisuals.CreateSphere(transform, 0.02f, Color.white, "Screen UR");
        DebugVisuals.SphereAt(go, cornerUR);
        go = DebugVisuals.CreateSphere(transform, 0.02f, Color.white, "Screen LL");
        DebugVisuals.SphereAt(go, cornerLL);
        go = DebugVisuals.CreateSphere(transform, 0.02f, Color.cyan, "Kinect Camera");
        DebugVisuals.SphereAt(go, Vector3.zero);
        go = DebugVisuals.CreateSphere(transform, 0.02f, Color.green, "Screen LR");
        DebugVisuals.SphereAt(go, cornerLR);

        go = DebugVisuals.CreateCylinder(transform, 0.01f, Color.red, $"Line {counter}");
        DebugVisuals.CylinderBetween(go, cornerUL, cornerUR);
        go = DebugVisuals.CreateCylinder(transform, 0.01f, Color.red, $"Line {counter}");
        DebugVisuals.CylinderBetween(go, cornerUR, cornerLR);
        go = DebugVisuals.CreateCylinder(transform, 0.01f, Color.red, $"Line {counter}");
        DebugVisuals.CylinderBetween(go, cornerLR, cornerLL);
        go = DebugVisuals.CreateCylinder(transform, 0.01f, Color.red, $"Line {counter}");
        DebugVisuals.CylinderBetween(go, cornerLL, cornerUL);
    }

    // point at depth = z al    
    private Vector3 pointAtZ(Vector3 p1, Vector3 A, float z)
    {
        Vector3 direction = A - p1;
        float t = (z - p1.z) / direction.z;
        Vector3 point = p1 + t * direction;
        return point;
    }

    // intersection point between two vectors given as base point + direction
    private Vector3 intersectionPoint(Vector3 p1, Vector3 p2, Vector3 A, Vector3 B)
    {
        // code generated by chatGPT
        float t, s;
        Vector3 intersectionPoint = Vector3.zero;

        Vector3 crossProduct = Vector3.Cross(A, -B);
        float denominator = crossProduct.magnitude * crossProduct.magnitude;

        if (denominator != 0)
        {
            Vector3 difference = p2 - p1;

            t = Vector3.Dot(Vector3.Cross(difference, -B), crossProduct) / denominator;
            s = Vector3.Dot(Vector3.Cross(A, difference), crossProduct) / denominator;

            intersectionPoint = p1 + t * A;
        }
        else
        {
            Debug.Log("Lines are parallel!");
        }

        return intersectionPoint;
    }
}
