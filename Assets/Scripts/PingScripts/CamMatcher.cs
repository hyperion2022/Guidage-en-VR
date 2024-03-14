using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMatcher : MonoBehaviour
{
    public Camera MainCamera;
    public Camera SecondCamera;
    // Start is called before the first frame update
    void Start()
    {
        SecondCamera.transform.rotation = MainCamera.transform.rotation;
        SecondCamera.transform.position = MainCamera.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        SecondCamera.transform.rotation = MainCamera.transform.rotation;
        SecondCamera.transform.position = MainCamera.transform.position;
    }
}
