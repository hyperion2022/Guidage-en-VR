using UnityEngine;
using UnityEngine.Assertions;

// https://docs.unity3d.com/Manual/CameraRays.html
// https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html
public class ObjectSelector : MonoBehaviour
{
    [SerializeField] bool hovering = false;
    [SerializeField] float sizeLimit = 10f;
    [SerializeField] new Camera camera;
    [SerializeField] BodyPointsProvider bodyPointsProvider = null;
    [SerializeField] string calibrationFilePath = "calibration.json";
    [SerializeField] RectTransform topLeft;
    [SerializeField] RectTransform cursor;

    public class SelectedObject : Outline
    {
        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                OutlineColor = value ? Color.yellow : Color.white;
            }
        }
        private bool selected;

        public SelectedObject()
        {
            OutlineMode = Mode.OutlineAll;
            OutlineWidth = 10f;
            selected = false;
        }
    }

    private Calibration calibration = null;
    // when the mouse is hovering an object, we store a reference to it
    private SelectedObject hovered = null;
    private bool poiting = false;
    private Vector2 pointingAt = Vector2.zero;

    void Start()
    {
        Assert.IsNotNull(camera);
        try { calibration = Calibration.LoadFromFile(calibrationFilePath); }
        catch { }
        if (bodyPointsProvider != null)
        {
            bodyPointsProvider.BodyPointsChanged += OnBodyPointsChange;
        }
    }
    private void PlaceOnCanvasFromNormalizedPos(RectTransform rectTransform, Vector2 pos)
    {
        pos *= 2f;
        pos -= Vector2.one;
        pos.Scale(-topLeft.localPosition);
        rectTransform.localPosition = new(pos.x, pos.y, rectTransform.localPosition.z);
    }

    void OnBodyPointsChange()
    {
        var (valid, pos) = calibration.PointingAt(bodyPointsProvider);
        if (valid != poiting) cursor.gameObject.SetActive(valid);
        poiting = valid;
        if (valid)
        {
            PlaceOnCanvasFromNormalizedPos(cursor, pos);
            pointingAt = new(pos.x * camera.pixelWidth, (1f - pos.y) * camera.pixelHeight);
        }
    }

    void Update()
    {
        bool interact = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Mouse0);
        if (!poiting) pointingAt = (Vector2)Input.mousePosition;
        if (hovering) UpdatePointedObject(pointingAt, interact);
        else if (interact) UpdatePointedObject(pointingAt);
    }

    void UpdatePointedObject(Vector2 screenPos, bool interact)
    {
        if (Physics.Raycast(camera.ScreenPointToRay(screenPos), out var hit) && SizeIsInferiorToLimit(hit.transform, sizeLimit))
        {
            // if the hovered object is no longer being pointed at, then remove outline if not selected
            if (
                hovered != null &&
                hovered.transform != hit.transform &&
                !hovered.Selected
            ) Destroy(hovered);

            // whatever is being pointed at, get the Outline (or in our case SelectedObject) component
            // if the component is not present, then add it (by default is not selected)
            if (!hit.transform.TryGetComponent(out hovered))
            {
                hovered = hit.transform.gameObject.AddComponent<SelectedObject>();
            }

            // if we are clicking, toogle selection on hovered object
            if (interact) hovered.Selected = !hovered.Selected;
        }
        else// if we are not pointing at anything
        {
            // but we previously had a hovered object, remove the hover and the Outline if not selected
            if (hovered != null && !hovered.Selected) Destroy(hovered);
            hovered = null;
        }
    }

    void UpdatePointedObject(Vector2 screenPos)
    {
        if (Physics.Raycast(camera.ScreenPointToRay(screenPos), out var hit) && SizeIsInferiorToLimit(hit.transform, sizeLimit))
        {
            if (hit.transform.TryGetComponent(out SelectedObject outline))
            {
                Destroy(outline);
            }
            else
            {
                hit.transform.gameObject.AddComponent<SelectedObject>().Selected = true;
            }
        }
    }

    static bool SizeIsInferiorToLimit(Transform go, float limit)
    {
        var bounds = new Bounds(go.position, Vector3.zero);
        foreach (var collider in go.GetComponents<Collider>()) bounds.Encapsulate(collider.bounds);
        var size = bounds.size.magnitude;
        return size > 0f && size < limit;
    }
}
