using UnityEngine;

// https://docs.unity3d.com/Manual/CameraRays.html
// https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html
public class ObjectSelector : MonoBehaviour
{
    public Camera mainCamera;
    private GameObject lastHitObject;
    private GameObject newHitObject;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void FixedUpdate()
    {
        DrawRayFromMousePosition();
    }

    void DrawRayFromMousePosition()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Launch ray
        if (Physics.Raycast(ray, out hit))
        {
            newHitObject = hit.transform.gameObject;

            // If we hit a new object different from the last one
            if (lastHitObject != newHitObject)
            {
                // Remove the outline on the last object
                if (lastHitObject != null)
                {
                    Destroy(lastHitObject.GetComponent<Outline>());
                }
            
                // Do something with the object that was hit by the raycast.
                // Color color = Color.red;
                // Debug.DrawLine(ray.origin, hit.point, color);
                // Debug.DrawLine(ray.origin, hit.point + new Vector3(0.25f, 0.25f, 0.25f), color);
                // Debug.Log(objectHit);

                // Add the outline to the new object
                var newHitObjectOutline = newHitObject.AddComponent<Outline>();
                newHitObjectOutline.OutlineMode = Outline.Mode.OutlineAll;
                newHitObjectOutline.OutlineColor = Color.yellow;
                newHitObjectOutline.OutlineWidth = 10f;

                // The new object becomes the last object.
                lastHitObject = newHitObject;
            }
        }
    }
}
