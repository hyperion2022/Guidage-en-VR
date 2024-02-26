using System.Collections.Generic;
using UnityEngine;
using BodyPoint = BodyPointsProvider.BodyPoint;

public class BodyPointsVisualizer : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;

    Dictionary<BodyPoint, Transform> nodes;

    void Start()
    {
        var node = transform.Find("Node");
        nodes = new Dictionary<BodyPoint, Transform>();
        foreach (var k in bodyPointsProvider.AvailablePoints) {
            nodes[k] = Instantiate(node, transform);
            nodes[k].name = k.ToString();
        }
        node.GetComponent<Renderer>().enabled = false;
        bodyPointsProvider.BodyPointsChanged += NewPoints;
    }

    private static Color trackingStateToColor(float ts) {
        return ts switch {
            1f => Color.Lerp(Color.green, Color.white, 0.5f),
            0f => Color.Lerp(Color.white, Color.white, 0.5f),
            3f => Color.Lerp(Color.red, Color.white, 0.5f),
            _ => Color.Lerp(Color.blue, Color.white, 0.5f),
        };
    }

    void OnDestroy() {
        bodyPointsProvider.BodyPointsChanged -= NewPoints;
    }

    void NewPoints()
    {
        foreach (var (k, t) in nodes) {
            var v = bodyPointsProvider.GetBodyPoint(k);
            t.localPosition = (Vector3)v * 5f;
            t.GetComponent<Renderer>().material.color = trackingStateToColor(v.w);
        }
    }
}
