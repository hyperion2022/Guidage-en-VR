using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingManager : MonoBehaviour
{
    [SerializeField] public GameObject worldpingprefab;
    [SerializeField] public float offset = 0.1f;
    private ScreenPointing screenPointing;
    void Start()
    {
        screenPointing = GetComponent<ScreenPointing>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("mouse 2"))
        {
            if (screenPointing.pointing.mode != ScreenPointing.PointingMode.None)
            {
                Ray ray = screenPointing.pointingCamera.ScreenPointToRay(screenPointing.pointing.atPixel);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider != null)
                    {
                        if (hit.collider.gameObject.CompareTag("ping"))
                        {
                            removePing(hit.collider.gameObject);
                        }
                        else
                        {
                            addPing(new Vector3(hit.point.x, hit.point.y, hit.point.z - offset));
                        }
                    }

                }
            }

        }
    }
    public void addPing(Vector3 position)
    {
        Instantiate(worldpingprefab, position, Quaternion.identity);
    }
    public void removePing(GameObject ping)
    {
        Destroy(ping);
    }
}
