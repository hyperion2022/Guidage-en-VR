using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

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

    private void PlaceOnCanvasFromNormalizedPos(RectTransform rectTransform, Vector2 pos)
    {
        pos *= 2f;
        pos -= Vector2.one;
        pos.Scale(-bottomLeft.localPosition);
        rectTransform.localPosition = new(pos.x, pos.y, rectTransform.localPosition.z);
    }
    private Vector2 WorldToNormalizedScreenPos(Vector3 worldPos)
    {
        var pos = (Vector2)screenPointing.targetCamera.WorldToScreenPoint(worldPos);
        return new(pos.x / screenPointing.targetCamera.pixelWidth, pos.y / screenPointing.targetCamera.pixelHeight);
    }
    void Update()
    {
        // if place/remove triggered (mouse middle click, or keyboard P)
        if (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.P))
        {
            if (screenPointing.pointing.mode != ScreenPointing.PointingMode.None)
            {
                // detect if there are UI elements under the pointing
                // we use a raycast
                var removed = false;
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(
                    new PointerEventData(EventSystem.current) { position = screenPointing.pointing.atPixel },
                    hits
                );
                foreach (var canvasHit in hits)
                {
                    if (canvasHit.gameObject.TryGetComponent<RectTransform>(out var rect))
                    {
                        // check if it is a Ping
                        if (pings.ContainsKey(rect))
                        {
                            // then remove it
                            pings.Remove(rect);
                            Destroy(canvasHit.gameObject);
                            removed = true;
                        }
                    }
                }

                // if no Ping where under the pointing, then the user wants to place a new Ping
                if (!removed)
                {
                    // where does it lends in the world space
                    Ray ray = screenPointing.targetCamera.ScreenPointToRay(screenPointing.pointing.atPixel);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        var pos = WorldToNormalizedScreenPos(hit.point);
                        // check for good measures it is not outside the screen
                        if (pos.x >= 0f && pos.x <= 1f && pos.y >= 0f && pos.y <= 1f)
                        {
                            // creat a Ping
                            var go = Instantiate(pingPrefab, transform).GetComponent<RectTransform>();
                            go.gameObject.SetActive(true);
                            pings.Add(go, hit.point);
                        }
                    }

                }
            }

        }

        // place correctly the UI images
        foreach (var (ping, pos) in pings)
        {
            var screenPos = WorldToNormalizedScreenPos(pos);
            PlaceOnCanvasFromNormalizedPos(ping, screenPos);
        }
    }
}
