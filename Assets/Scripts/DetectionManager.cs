using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using static BodyPointsProvider;

public class DetectionManager : MonoBehaviour
{
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] GameObject cursor;
    private Vector3[] screenPoints;
    private float screenWidth, screenHeight;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        screenPoints = JsonConvert.DeserializeObject<float[][]>(File.ReadAllText("screenCorners.json")).Select(v=>new Vector3(v[0], v[1], v[2])).ToArray();
        Debug.Log("Detection Manager Start");

        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 pointedPixel = Detection();
        Vector3 cursorPosition = new Vector3(screenWidth * pointedPixel.x, screenHeight * pointedPixel.y, 0);
        cursor.transform.position = cursorPosition; // cam.ScreenToWorldPoint(cursorPosition);
    }

    private Vector2 Detection()
    {
        var head = bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
        var rightIndex = bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);

        Vector3 pointOnScreen = pointAtZ(head, rightIndex, screenPoints[0].z);
        float posX = (pointOnScreen.x - screenPoints[0].x) / (screenPoints[1].x - screenPoints[0].x);
        float posY = (pointOnScreen.y - screenPoints[0].y) / (screenPoints[2].y - screenPoints[0].y);

        // pos sur l'�cran entre 0 et 1
        return new Vector2(posX, posY);
    }

    // point at depth = z al    
    private Vector3 pointAtZ(Vector3 p1, Vector3 A, float z)
    {
        Vector3 direction = A - p1;
        float t = (z - p1.z) / direction.z;
        Vector3 point = p1 + t * direction;
        return point;
    }
}
