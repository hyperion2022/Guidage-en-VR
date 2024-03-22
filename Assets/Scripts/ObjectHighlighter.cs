using UnityEngine;
using UnityEngine.Assertions;

public class ObjectHighlighter : MonoBehaviour
{
    [SerializeField] bool hovering = false;
    [SerializeField] float sizeLimit = 10f;
    public class Highlight : Outline
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

        public Highlight()
        {
            OutlineMode = Mode.OutlineAll;
            OutlineWidth = 10f;
            selected = false;
        }
    }
    private ScreenPointing screenPointing;

    // when the mouse is hovering an object, we store a reference to it
    private Highlight hovered = null;
    void Start()
    {
        screenPointing = GetComponent<ScreenPointing>();
        Assert.IsNotNull(screenPointing);
    }
    void Update()
    {
        bool interact = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Mouse0);
        if (screenPointing.pointing.mode == ScreenPointing.PointingMode.None) return;
        if (hovering) UpdatePointedObject(screenPointing.pointing.atPixel, interact);
        else if (interact) UpdatePointedObject(screenPointing.pointing.atPixel);
    }
    void UpdatePointedObject(Vector2 screenPos, bool interact)
    {
        if (Physics.Raycast(screenPointing.targetCamera.ScreenPointToRay(screenPos), out var hit) && SizeIsInferiorToLimit(hit.transform, sizeLimit))
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
                hovered = hit.transform.gameObject.AddComponent<Highlight>();
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
        if (Physics.Raycast(screenPointing.targetCamera.ScreenPointToRay(screenPos), out var hit) && SizeIsInferiorToLimit(hit.transform, sizeLimit))
        {
            if (hit.transform.TryGetComponent(out Highlight outline))
            {
                Destroy(outline);
            }
            else
            {
                hit.transform.gameObject.AddComponent<Highlight>().Selected = true;
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
