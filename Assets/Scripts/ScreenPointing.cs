using UnityEngine;
using UnityEngine.Assertions;

// https://docs.unity3d.com/Manual/CameraRays.html
// https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html
public class ScreenPointing : MonoBehaviour
{
    [SerializeField] public Camera pointingCamera;
    [SerializeField] BodyPointsProvider bodyPointsProvider = null;
    [SerializeField] string calibrationFilePath = "calibration.json";


    public enum PointingMode { Body, Mouse, None };
    public (PointingMode mode, Vector2 atPixel, Vector2 atNorm) pointing;

    private Calibration calibration = null;
    private float lastBodyPointing = -100f;

    void Start()
    {
        Assert.IsNotNull(pointingCamera);
        try { calibration = Calibration.LoadFromFile(calibrationFilePath); }
        catch { }
        if (bodyPointsProvider != null)
        {
            Assert.IsNotNull(calibration);
            bodyPointsProvider.BodyPointsChanged += OnBodyPointsChange;
        }
    }

    void OnBodyPointsChange()
    {
        var (valid, pos) = calibration.PointingAt(bodyPointsProvider);
        if (valid)
        {
            lastBodyPointing = Time.timeSinceLevelLoad;
            pointing.atNorm = new(pos.x, 1f - pos.y);
            pointing.atPixel = Vector2.Scale(pointing.atNorm, pointingCamera.pixelRect.size);
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
                pointing.atPixel.x / pointingCamera.pixelWidth,
                pointing.atPixel.y / pointingCamera.pixelHeight
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

