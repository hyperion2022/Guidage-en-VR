using UnityEngine;

// https://docs.unity3d.com/Manual/CameraRays.html
// https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html
public class ObjectSelector : MonoBehaviour
{
    // when the mouse is hovering an object, we store a reference to it
    private SelectedObject hovered = null;

    void Update()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
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
            if (Input.GetKeyDown(KeyCode.Mouse0)) hovered.Selected = !hovered.Selected;
        }
        else// if we are not pointing at anything
        {
            // but we previously had a hovered object, remove the hover and the Outline if not selected
            if (hovered != null && !hovered.Selected) Destroy(hovered);
            hovered = null;
        }
    }
}
