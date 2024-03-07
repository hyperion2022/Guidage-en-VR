using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using static BodyPointsProvider;

public class DetectionManager : MonoBehaviour
{
    [SerializeField] BodyPointsProvider bodyPointsProvider;
    [SerializeField] GameObject cursor;

    private (
        Vector3 tl,
        Vector3 tr,
        Vector3 bl,
        Vector3 i,
        Vector3 j,
        Vector3 normal,
        float width,
        float height
    ) screen;

    public CalibrationManager.Calibration Calibration
    {
        set
        {
            screen.tl = CalibrationManager.Calibration.ToVector3(value.tl);
            Debug.Log($"Loading {screen.tl}");
            screen.tr = CalibrationManager.Calibration.ToVector3(value.tr);
            screen.bl = CalibrationManager.Calibration.ToVector3(value.bl);
            screen.i = screen.tr - screen.tl;
            screen.j = screen.bl - screen.tl;
            screen.normal = Vector3.Cross(screen.i, screen.j).normalized;
        }
    }
    private Camera cam;
    private Visual.Sphere sphere1;
    private Visual.Sphere sphere2;

    private void Start()
    {
        cam = Camera.main;
        Calibration = JsonConvert.DeserializeObject<CalibrationManager.Calibration>(File.ReadAllText("calibration.json"));
        sphere1 = new(transform, 0.02f, Color.magenta, "Pointing at");
        sphere2 = new(transform, 0.03f, Color.yellow, "Screen Corner"){ At = screen.tl };
        Debug.Log("Detection Manager Start");

        screen.width = Screen.width;
        screen.height = Screen.height;

        bodyPointsProvider.BodyPointsChanged += UpdatePointer;
    }

    // Update is called once per frame
    void UpdatePointer()
    {
        var (found, pointedPixel) = Detection();
        if (found)
        {
            Debug.Log($"Pointing at {pointedPixel}");
            Vector3 cursorPosition = new Vector3(screen.width * pointedPixel.x, screen.height * pointedPixel.y, 0);
            cursor.transform.position = cursorPosition; // cam.ScreenToWorldPoint(cursorPosition);
        }
    }

    private (bool, Vector2) Detection()
    {
        var head = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.Head);
        var index = (Vector3)bodyPointsProvider.GetBodyPoint(BodyPoint.RightIndex);
        var (found, point) = CalibrationManager.EdgeOnPlaneIntersection(head, (index - head).normalized, screen.tl, screen.normal);
        if (!found) return (false, Vector2.zero);
        sphere1.At = point;
        point -= screen.tl;
        return (true, new(
            Vector3.Dot(screen.i, point),
            Vector3.Dot(screen.j, point)
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
    private Vector3 pointAtZ(Vector3 p1, Vector3 A, float z)
    {
        Vector3 direction = A - p1;
        float t = (z - p1.z) / direction.z;
        Vector3 point = p1 + t * direction;
        return point;
    }
}
