using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using static BodyPointsProvider;

public class BodyPointsRecorder : MonoBehaviour
{
    [SerializeField]
    BodyPointsProvider bodyPointsProvider;

    [SerializeField] float capturesPerSecond = 15f;
    [SerializeField] string outputFilePath = "recorded-body-points.json";

    private bool waitCaptation;

    private List<Vector4[]> recorded;
    public void Start()
    {
        waitCaptation = true;
        recorded = new List<Vector4[]>();
        InvokeRepeating("CallBack", 0f, 1f / capturesPerSecond);
        // Debug.Log(JsonConvert.SerializeObject(recorded));
    }

    public void CallBack()
    {
        var points = bodyPointsProvider.AvailablePoints.Select(bodyPointsProvider.GetBodyPoint).ToArray();
        if (waitCaptation)
        {
            foreach (var point in points)
            {
                if (IsTracked(point)) waitCaptation = false;
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
                columns = bodyPointsProvider.AvailablePoints.Select(v => v.ToString()).ToArray(),
                data = recorded.Select(v => v.Select(v => new float[] { v.x, v.y, v.z, v.w }).ToArray()).ToArray(),
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
