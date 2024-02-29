using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


public class PingManager : MonoBehaviour
{
	
    public Camera mainCamera;
    public GameObject ping1;
    public GameObject ping2;
    [SerializeField] public float offset=0.1f;
    public Texture2D cursor;
    //ping AudioClip audio1;
    private void Start()
    {
        Cursor.SetCursor(cursor, new Vector2(16, 16), CursorMode.Auto);
    }
    // Update is called once per frame
    void Update()
    {
      if (Input.GetKeyDown("mouse 2"))
            {
	       Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
	       if(Physics.Raycast(ray,out RaycastHit hit))
	                {
                      
                      Instantiate(ping1,new Vector3(hit.point.x,hit.point.y,hit.point.z-offset),Quaternion.identity);
	                //AudioSource.PlayClipAtPoint(audio1,hit.point);
	                }
	        } 
    }
}
