using UnityEngine;
using UnityEngine.Assertions;

// https://docs.unity3d.com/Manual/CameraRays.html
// https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html
public class ScreenPointing : MonoBehaviour
{
    [SerializeField] float smoothFactor = 0.5f;
    [SerializeField] public Camera targetCamera;
    [SerializeField] BodyPointsProvider bodyPointsProvider = null;
    [SerializeField] string calibrationFilePath = "calibration.json";


    public enum PointingMode { Body, Mouse, None };
    public (PointingMode mode, Vector2 atPixel, Vector2 atNorm) pointing;

    private Calibration calibration = null;
    private float lastBodyPointing = -100f;
    private Vector2 bodySmooth;

    void Start()
    {
        Assert.IsTrue(smoothFactor >= 0f && smoothFactor <= 0.9f);
        bodySmooth = Vector2.zero;
        var canvas = GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        canvas.targetDisplay = targetCamera.targetDisplay;
        Assert.IsNotNull(targetCamera);
        try { calibration = Calibration.LoadFromFile(calibrationFilePath); }
        catch { }
        if (bodyPointsProvider != null)
        {
            Assert.IsNotNull(calibration);
            bodyPointsProvider.BodyPointsChanged += OnBodyPointsChange;
        }
    }

    private Vector2 BodySmooth(Vector2 pos)
    {
        if (pointing.mode != PointingMode.Body) bodySmooth = pos;
        bodySmooth = smoothFactor * bodySmooth + (1f - smoothFactor) * pos;
        return bodySmooth;
    }

    void OnBodyPointsChange()
    {
        var (valid, pos) = calibration.PointingAt(bodyPointsProvider);
        if (valid)
        {
            lastBodyPointing = Time.timeSinceLevelLoad;
            pointing.atNorm = BodySmooth(new(pos.x, 1f - pos.y));
            // pointing.atNorm = new(pos.x, 1f - pos.y);
            pointing.atPixel = Vector2.Scale(pointing.atNorm, targetCamera.pixelRect.size);
            pointing.mode = PointingMode.Body;
        }
    }

    void FixedUpdate()
    {
        // if body pointing is not performed for longer than 0.5 seconds, then use mouse pointing
        if (Time.timeSinceLevelLoad - lastBodyPointing > 0.5f)
        {
            pointing.atPixel = (Vector2)Input.mousePosition;
            pointing.atNorm = new(
                pointing.atPixel.x / targetCamera.pixelWidth,
                pointing.atPixel.y / targetCamera.pixelHeight
            );
            // if the mouse is not inside the display rect, then ignore it
            pointing.mode = (
                pointing.atNorm.x >= 0f &&
                pointing.atNorm.x <= 1f &&
                pointing.atNorm.y >= 0f &&
                pointing.atNorm.y <= 1f
            ) ? PointingMode.Mouse : PointingMode.None;
        }
    }
}

