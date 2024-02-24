using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Key = BodyPointsProvider.Key;

public class BodyPointsVisualizer : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;

    // Transform head;
    // Transform leftWrist;
    // Transform rightWrist;
    // Transform leftIndex;
    // Transform rightIndex;
    Dictionary<Key, Transform> nodes;

    void Start()
    {
        var node = transform.Find("Node");
        nodes = new Dictionary<Key, Transform>();
        foreach (var k in bodyPointsProvider.AvailablePoints) {
            nodes[k] = Instantiate(node, transform);
            nodes[k].name = k.ToString();
        }
        node.GetComponent<Renderer>().enabled = false;
        // head = transform.Find("Head");
        // leftWrist = transform.Find("Left Wrist");
        // rightWrist = transform.Find("Right Wrist");
        // leftIndex = transform.Find("Left Index");
        // rightIndex = transform.Find("Right Index");
    }

    private static Color trackingStateToColor(float ts) {
        return ts switch {
            1f => Color.Lerp(Color.green, Color.white, 0.5f),
            0f => Color.Lerp(Color.white, Color.white, 0.5f),
            3f => Color.Lerp(Color.red, Color.white, 0.5f),
            _ => Color.Lerp(Color.blue, Color.white, 0.5f),
        };
    }

    void Update()
    {
        // var body = bodyPointsProvider.GetBodyPoints();
        foreach (var k in bodyPointsProvider.AvailablePoints)
        {
            var v = bodyPointsProvider.GetBodyPoint(k);
            nodes[k].position = v * 5f;
            nodes[k].GetComponent<Renderer>().material.color = trackingStateToColor(v.w);
        }
        // head.position = body.head * 5f;
        // head.GetComponent<Renderer>().material.color = trackingStateToColor(body.head.w);
        // leftWrist.position = body.leftWrist * 5f;
        // leftWrist.GetComponent<Renderer>().material.color = trackingStateToColor(body.leftWrist.w);
        // rightWrist.position = body.rightWrist * 5f;
        // rightWrist.GetComponent<Renderer>().material.color = trackingStateToColor(body.rightWrist.w);
        // leftIndex.position = body.leftIndex * 5f;
        // leftIndex.GetComponent<Renderer>().material.color = trackingStateToColor(body.leftIndex.w);
        // rightIndex.position = body.rightIndex * 5f;
        // rightIndex.GetComponent<Renderer>().material.color = trackingStateToColor(body.rightIndex.w);
    }
}
