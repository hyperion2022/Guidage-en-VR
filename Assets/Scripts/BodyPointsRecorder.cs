using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using static BodyPointsProvider;
using System;

public class BodyPointsRecorder : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;

    [SerializeField] float capturesPerSecond = 15f;
    [SerializeField] string outputFilePath = "recorded-body-points.json";

    private bool waitCaptation;

    private List<(PointState state, Vector3 pos)[]> recorded;
    public void Start()
    {
        waitCaptation = true;
        recorded = new List<(PointState, Vector3)[]>();
        InvokeRepeating("CallBack", 0f, 1f / capturesPerSecond);
        // Debug.Log(JsonConvert.SerializeObject(recorded));
    }

    public void CallBack()
    {
        var points = bodyPointsProvider.ProvidedPoints.Select(bodyPointsProvider.GetBodyPoint).ToArray();
        if (waitCaptation)
        {
            foreach (var point in points)
            {
                if (point.state == PointState.Tracked) waitCaptation = false;
            }
        }
        else
        {
            recorded.Add(points);
        }
    }

    public void OnDestroy()
    {
        if (recorded.Count > 0)
        {
            File.WriteAllText(outputFilePath, JsonConvert.SerializeObject(new Recorded
            {
                hertz = capturesPerSecond,
                columns = bodyPointsProvider.ProvidedPoints.Select(v => v.ToString()).ToArray(),
                data = recorded.Select(v => v.Select(v => new float[] { v.pos.x, v.pos.y, v.pos.z, v.state switch {
                    PointState.NotProvided => 0f,
                    PointState.Tracked => 1f,
                    PointState.Inferred => 2f,
                    PointState.NotTracked => 3f,
                    _ => throw new InvalidOperationException()
                }}).ToArray()).ToArray(),
            }));
        }
    }

    [System.Serializable]
    private struct Recorded
    {
        public float hertz;
        public string[] columns;
        public float[][][] data;
    }
}
