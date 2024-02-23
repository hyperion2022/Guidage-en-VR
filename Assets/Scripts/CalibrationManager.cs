using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;
using static BodyPointsProvider;

public class CalibrationManager : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;
    public GameObject[] corners;
    public GameObject center;
    public Text instructionText;
    private int counter;
    private string message;
    private BodyPoints[] bodyPoints;
    private Vector3[] screenPoints;

    // Start is called before the first frame update
    void Start()
    {
        if (corners.Length != 4)
        {
            Debug.Log("Error: Only 4 corners allowed!");
        }

        foreach (var corner in corners)
        {
            corner.GetComponent<Image>().color = Color.red;
        }

        bodyPoints = new BodyPoints[6];
        screenPoints = new Vector3[3];

        counter = 0;
        message = "0. Stand up so that the Kinect can detect all your body and validate when you are ready.";
        instructionText.text = message;
    }

    private Vector2 Detection()
    {
        BodyPoints currentBodyPoints = bodyPointsProvider.GetBodyPoints();

        Vector3 pointOnScreen = pointAtZ(currentBodyPoints.rightWrist, currentBodyPoints.rightIndex, screenPoints[0].z);
        float posX = (pointOnScreen.x - screenPoints[0].x) / (screenPoints[1].x - screenPoints[0].x);
        float posY = (pointOnScreen.y - screenPoints[0].y) / (screenPoints[2].y - screenPoints[0].y);

        // pos sur l'�cran entre 0 et 1
        return new Vector2 (posX, posY);
    }

    public void UpdateCorner()
    {
        // if we validate one of the 4 corners
        if (counter > 0 && counter <= 4)
        {
            corners[counter - 1].GetComponent<Image>().color = Color.green;
        }
        // the 5th and 6th counters correspond to the center point
        if (counter == 6)
        {
            center.GetComponent<Image>().color = Color.green;
        }
        // we save 6 positions = 4 corners + 2 center directions
        if (counter >= 1 && counter <= 6)
        {
            // bodyPoints[i - 1] = GetBodyPoints();
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
            case 8: CornerCoordsFromBodyPoints(bodyPoints); break; // d�sactiver l'interface de calibration
            default: message = "Error!";  break;
        }

        instructionText.text = message;
    }

    private void CornerCoordsFromBodyPoints(BodyPoints[] bodyPoints)
    {
        if (bodyPoints.Length != 6)
        {
            Debug.Log("bodyPoints array has to be of length 6!");
        }

        // prendre en compte la 4e valeur
        // factoriser les points choisis
        Vector4 dirLeftCenter = bodyPoints[4].rightIndex - bodyPoints[4].rightWrist;
        Vector4 dirRightCenter = bodyPoints[5].rightIndex - bodyPoints[5].rightWrist;
        Vector3 p1 = new Vector3(bodyPoints[4].rightWrist.x, bodyPoints[4].rightWrist.y, bodyPoints[4].rightWrist.z);
        Vector3 p2 = new Vector3(bodyPoints[5].rightWrist.x, bodyPoints[5].rightWrist.y, bodyPoints[5].rightWrist.z);
        Vector3 A = new Vector3(dirLeftCenter.x, dirLeftCenter.y, dirLeftCenter.z);
        Vector3 B = new Vector3(dirRightCenter.x, dirRightCenter.y, dirRightCenter.z);
        Vector3 centerPoint = intersectionPoint(p1, p2, A, B);

        // 3 points forment un plan/rectangle, le 4e peut sortir de ce plan � cause d'impr�cisions de calcul
        Vector3 cornerUL = pointAtZ(bodyPoints[0].rightWrist, bodyPoints[0].rightIndex, centerPoint.z);
        Vector3 cornerUR = pointAtZ(bodyPoints[1].rightWrist, bodyPoints[1].rightIndex, centerPoint.z);
        Vector3 cornerLR = pointAtZ(bodyPoints[2].rightWrist, bodyPoints[2].rightIndex, centerPoint.z);

        // sortie: rectangle (3 points)
        screenPoints[0] = cornerUL;
        screenPoints[1] = cornerUR;
        screenPoints[2] = cornerLR;
    }

    private Vector3 pointAtZ(Vector3 p1, Vector3 A, float z) 
    {
        Vector3 direction = A - p1;
        float t = (z - p1.z) / direction.z;
        Vector3 point = p1 + t * direction;
        return point;
    }

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
