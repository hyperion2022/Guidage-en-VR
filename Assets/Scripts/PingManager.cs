using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PingManager : MonoBehaviour
{
    [SerializeField] GameObject pingPrefab;
    [SerializeField] RectTransform bottomLeft;
    public Dictionary<RectTransform, Vector3> Pings => pings;
    private ScreenPointing screenPointing;

    private Dictionary<RectTransform, Vector3> pings;
    void Start()
    {
        Assert.IsNotNull(pingPrefab);
        Assert.IsNotNull(bottomLeft);
        screenPointing = GetComponent<ScreenPointing>();
        pings = new();
    }

    public void OnPingClick(RectTransform go)
    {
        pings.Remove(go);
        Destroy(go.gameObject);
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
        var pos = (Vector2)screenPointing.pointingCamera.WorldToScreenPoint(worldPos);
        return new(pos.x / screenPointing.pointingCamera.pixelWidth, pos.y / screenPointing.pointingCamera.pixelHeight);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.P))
        {
            if (screenPointing.pointing.mode != ScreenPointing.PointingMode.None)
            {
                Ray ray = screenPointing.pointingCamera.ScreenPointToRay(screenPointing.pointing.atPixel);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var pos = WorldToNormalizedScreenPos(hit.point);
                    if (pos.x >= 0f && pos.x <= 1f && pos.y >= 0f && pos.y <= 1f)
                    {
                        var go = Instantiate(pingPrefab, transform).GetComponent<RectTransform>();
                        go.gameObject.SetActive(true);
                        pings.Add(go, hit.point);
                    }
                }
            }

        }

        foreach (var (ping, pos) in pings)
        {
            var screenPos = WorldToNormalizedScreenPos(pos);
            PlaceOnCanvasFromNormalizedPos(ping, screenPos);
        }
    }
}
