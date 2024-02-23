using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BodyPointsReplayer : BodyPointsProvider
{
    [SerializeField] string inputFilePath = "recorded-body-points.json";

    
    Recorded recorded;
    int i = 0;
    BodyPoints bodyPoints;

    public override BodyPoints GetBodyPoints() => bodyPoints;

    void Start()
    {
        recorded = JsonUtility.FromJson<Recorded>(File.ReadAllText(inputFilePath));
        InvokeRepeating("CallBack", 0f, 1f / recorded.rate);
    }
    void CallBack() {
        bodyPoints = recorded.recs[i % recorded.recs.Length];
        i += 1;
        EmitBodyPointsUpdatedEvent(bodyPoints);
    }


    [System.Serializable]
    private struct Recorded
    {
        public float rate;
        public BodyPoints[] recs;
    }
}
