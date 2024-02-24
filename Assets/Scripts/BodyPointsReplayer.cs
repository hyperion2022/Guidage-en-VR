using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Body = System.Collections.Generic.Dictionary<BodyPointsProvider.Key, UnityEngine.Vector4>;

public class BodyPointsReplayer : BodyPointsProvider
{
    [SerializeField] string inputFilePath = "recorded-body-points.json";

    
    Recorded recorded;
    int i = 0;
    Body bodyPoints;

    public override Key[] AvailablePoints => recorded.available;
    public override Vector4 GetBodyPoint(Key key) => bodyPoints[key];

    void Start()
    {
        recorded = JsonUtility.FromJson<Recorded>(File.ReadAllText(inputFilePath));
        InvokeRepeating("CallBack", 0f, 1f / recorded.rate);
    }
    void CallBack() {
        bodyPoints = recorded.recs[i % recorded.recs.Length];
        i += 1;
        EmitBodyPointsUpdatedEvent();
    }


    [System.Serializable]
    private struct Recorded
    {
        public float rate;
        public Key[] available;
        public Body[] recs;
    }
}
