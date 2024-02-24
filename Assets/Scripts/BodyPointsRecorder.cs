using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Body = System.Collections.Generic.Dictionary<BodyPointsProvider.Key, UnityEngine.Vector4>;

public class BodyPointsRecorder : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;

    [SerializeField] float capturesPerSecond = 15f;
    [SerializeField] float waitBeforeStart = 1f;
    [SerializeField] string outputFilePath = "recorded-body-points.json";

    private List<Body> recorded;
    public void Start()
    {
        recorded = new List<Body>();
        InvokeRepeating("CallBack", waitBeforeStart, 1f / capturesPerSecond);
    }

    public void CallBack()
    {
        var body = new Body();
        foreach (var k in bodyPointsProvider.AvailablePoints)
        {
            body[k] = bodyPointsProvider.GetBodyPoint(k);
        }
        recorded.Add(body);
    }

    public void OnDestroy()
    {
        File.WriteAllText(outputFilePath, Json.DictWriter
            .Field("rate", capturesPerSecond.ToString())
            .Field("available", Json.ListToJson(
                bodyPointsProvider.AvailablePoints,
                BodyPointsProvider.KeyToJson
            ))
            .Field("recs", Json.ListToJson(recorded, rec =>
                Json.DictToJson(rec, BodyPointsProvider.KeyToJson, v => Json.AnyToJson(v))
            ))
            .ToJson()
        );
    }

    // [System.Serializable]
    // private struct Recorded
    // {
    //     public float rate;
    //     public string[] available;
    //     public Body[] recs;
    // }
}
