using System.Collections.Generic;
using UnityEngine;
using BodyPoint = BodyPointsProvider.BodyPoint;

public class BodyPointsVisualizer : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;

    Dictionary<BodyPoint, GameObject> nodes;

    Dictionary<BodyPoint, BodyPoint> bones = new()
    {
        [BodyPoint.Head] = BodyPoint.LeftWrist,
    };

    void Start()
    {
        // var node = transform.Find("Node");
        nodes = new Dictionary<BodyPoint, GameObject>();
        foreach (var k in bodyPointsProvider.AvailablePoints)
        {
            nodes[k] = DebugVisuals.CreateSphere(0.5f, Color.white, k.ToString());
        }
        // node.GetComponent<Renderer>().enabled = false;
        bodyPointsProvider.BodyPointsChanged += NewPoints;
    }

    private static Color trackingStateToColor(float ts)
    {
        return ts switch
        {
            1f => Color.Lerp(Color.green, Color.white, 0.5f),
            0f => Color.Lerp(Color.white, Color.white, 0.5f),
            3f => Color.Lerp(Color.red, Color.white, 0.5f),
            _ => Color.Lerp(Color.blue, Color.white, 0.5f),
        };
    }

    void OnDestroy()
    {
        bodyPointsProvider.BodyPointsChanged -= NewPoints;
    }

    void NewPoints()
    {
        foreach (var (k, go) in nodes)
        {
            var v = bodyPointsProvider.GetBodyPoint(k);
            DebugVisuals.SphereAt(go, (Vector3)v * 5f);
            go.GetComponent<Renderer>().material.color = trackingStateToColor(v.w);
        }
    }
}
