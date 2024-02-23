using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Body = BodyPointsProvider.BodyPoints;

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
        recorded.Add(bodyPointsProvider.GetBodyPoints());
    }

    public void OnDestroy()
    {
        File.WriteAllText(outputFilePath, JsonUtility.ToJson(new Recorded
        {
            rate = capturesPerSecond,
            recs = recorded.ToArray()
        }));
    }

    [System.Serializable]
    private struct Recorded
    {
        public float rate;
        public Body[] recs;
    }
}
