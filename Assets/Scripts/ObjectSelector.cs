using Unity.Burst.CompilerServices;
using UnityEngine;

// https://docs.unity3d.com/Manual/CameraRays.html
// https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html
public class ObjectSelector : MonoBehaviour
{
    public Camera mainCamera;
    private GameObject lastHitObject;
    private GameObject newHitObject;

    private void drawOutline(GameObject obj, Color color, float thickness)
    {
        var objectOutline = obj.AddComponent<Outline>();
        objectOutline.OutlineMode = Outline.Mode.OutlineAll;
        objectOutline.OutlineColor = color;
        objectOutline.OutlineWidth = thickness;
    }
    private void deleteOutline(GameObject obj)
    {
        Destroy(obj.GetComponent<Outline>());
    }
    void Start()
    {
        mainCamera = Camera.main;
       
    }

    void FixedUpdate()
    {
        DrawRayFromMousePosition();


        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Launch ray
        if (Physics.Raycast(ray, out hit) && Input.GetKeyDown("mouse 0"))
        {
            GameObject objHit= hit.transform.gameObject;
            if (objHit.GetComponent<Outline>() != null)
            {
                if (objHit.GetComponent<Outline>().OutlineColor == Color.yellow)
                {
                    deleteOutline(objHit);
                }

                if (objHit.GetComponent<Outline>().OutlineColor == Color.gray)
                {
                    deleteOutline(objHit);
                    drawOutline(objHit, Color.yellow, 10f);
                }
            }
         
        }
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
                if (lastHitObject != null && lastHitObject.GetComponent<Outline>().OutlineColor==Color.gray)
                {
                    deleteOutline(lastHitObject);
                }

                // Add the outline to the new object
                drawOutline(newHitObject, Color.gray,5f);

                // The new object becomes the last object.
                lastHitObject = newHitObject;
            }
        }
    }
}
