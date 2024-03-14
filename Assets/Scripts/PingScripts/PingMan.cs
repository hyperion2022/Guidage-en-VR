using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingMan : MonoBehaviour
{
  
    public Camera mainCamera;
    public PingWindow window;
    [SerializeField] public float offset = 0.1f;
    public Texture2D cursor;

    [SerializeField] public int maxnumber=10;
    [SerializeField] public GameObject worldpingprefab;

    public List<GameObject> pingList = new List<GameObject>();

    private GameObject oldGrayOutline;





    public void addPing(Vector3 position)
    {
        Instantiate(worldpingprefab, position, Quaternion.identity);
    }

    public void removePing(GameObject ping)
    {
        Destroy(ping);
    }

    public void addOutline(GameObject obj, Color color,float thickness)
    {
        var objOutline = obj.AddComponent<Outline>();
        objOutline.OutlineMode = Outline.Mode.OutlineAll;
        objOutline.OutlineColor = color;
        objOutline.OutlineWidth = thickness;

    }

    public void removeOutline(GameObject obj)
    {
        Destroy(obj.GetComponent<Outline>());
    }

    void Start()
    {
        Cursor.SetCursor(cursor, new Vector2(16, 16), CursorMode.Auto);
    }

    // Update is called once per frame
     void Update()
    {
        if (Input.GetKeyDown("mouse 2"))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if(hit.collider != null)
                {
                    if (hit.collider.gameObject.CompareTag("ping"))
                    {
                        removePing(hit.collider.gameObject);
                    }
                    else
                    {
                        addPing(new Vector3(hit.point.x, hit.point.y, hit.point.z - offset));
                        //window.addPing(new Vector3(hit.point.x, hit.point.y, hit.point.z));
                    }
                }
                
            }
            
        }

       
       

           

       
        
            
    }

    
}
