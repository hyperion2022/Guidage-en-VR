using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class PingLayer : MonoBehaviour
{
    [SerializeField] PingManager pingManager;
    [SerializeField] Camera targetCamera;
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject pingPrefab;
    [SerializeField] RectTransform bottomLeft;

    private List<RectTransform> pings;

    void Start()
    {
        Assert.IsNotNull(pingPrefab);
        Assert.IsNotNull(bottomLeft);
        Assert.IsNotNull(targetCamera);
        Assert.IsNotNull(canvas);
        Assert.IsNotNull(pingManager);
        pings = new();
    }

    private void PlaceOnCanvasFromNormalizedPos(RectTransform rectTransform, Vector2 pos)
    {
        pos *= 2f;
        pos -= Vector2.one;
        pos.Scale(-bottomLeft.localPosition);
        rectTransform.localPosition = new(pos.x, pos.y, rectTransform.localPosition.z);
    }
    private Vector2 WorldToNormalizedScreenPos(Vector3 worldPos)
    {
        Assert.IsNotNull(targetCamera);
        var pos = (Vector2)targetCamera.WorldToScreenPoint(worldPos);
        return new(pos.x / targetCamera.pixelWidth, pos.y / targetCamera.pixelHeight);
    }
    void Update()
    {
        var pingMan = pingManager.Pings;
        while (pings.Count < pingMan.Count)
        {
            var go = Instantiate(pingPrefab, transform).GetComponent<RectTransform>();
            go.gameObject.SetActive(true);
            pings.Add(go);
        }
        while (pings.Count > pingMan.Count)
        {
            var at = pings.Count - 1;
            Destroy(pings[at].gameObject);
            pings.RemoveAt(at);
        }
        foreach (var (pos, ping) in pingManager.Pings.Zip(pings, (pos, ping) => (pos.Value, ping)))
        {
            var screenPos = WorldToNormalizedScreenPos(pos);
            PlaceOnCanvasFromNormalizedPos(ping, screenPos);
        }
    }
}
