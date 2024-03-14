using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingUIHandler : MonoBehaviour
{
    public RectTransform rectTransform;
    private Vector3 pingPosition;
    public void setup(Vector3 pingPosition)
    {
        this.pingPosition = pingPosition;
        rectTransform = transform.GetComponent<RectTransform>();
    }
    
    // Update is called once per frame
    void Update()
    {
        Vector3 fromPosition = Camera.main.transform.position;
        Vector3 dir = (pingPosition - fromPosition).normalized;

        float uiRadius = 270f;
        rectTransform.anchoredPosition = dir* uiRadius;
    }
}
