using UnityEngine;
using UnityEngine.Assertions;

class CursorFeedBack : MonoBehaviour
{
    [SerializeField] RectTransform bottomLeft;
    [SerializeField] RectTransform cursor;

    private ScreenPointing screenPointing;

    void Start()
    {
        screenPointing = GetComponent<ScreenPointing>();
        Assert.IsNotNull(screenPointing);
    }
    private void PlaceOnCanvasFromNormalizedPos(RectTransform rectTransform, Vector2 pos)
    {
        pos *= 2f;
        pos -= Vector2.one;
        pos.Scale(-bottomLeft.localPosition);
        rectTransform.localPosition = new(pos.x, pos.y, rectTransform.localPosition.z);
    }

    void Update()
    {
        if (screenPointing.pointing.mode == ScreenPointing.PointingMode.Body)
        {
            cursor.gameObject.SetActive(true);
            PlaceOnCanvasFromNormalizedPos(cursor, screenPointing.pointing.atNorm);
        }
        else
        {
            cursor.gameObject.SetActive(false);
        }
    }
}