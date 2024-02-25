using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

public class BodyPointsReplayer : BodyPointsProvider
{
    [SerializeField] string inputFilePath = "recorded-body-points.json";

    
    Recorded recorded;
    int i = 0;
    float[][] bodyPoints;
    Dictionary<Key, int> available;

    public override Key[] AvailablePoints => available.Keys.ToArray();
    public override Vector4 GetBodyPoint(Key key) {
        var point = bodyPoints[available[key]];
        return new Vector4(point[0], point[1], point[2], point[3]);
    }

    void Start()
    {
        recorded = JsonConvert.DeserializeObject<Recorded>(File.ReadAllText(inputFilePath));
        available = recorded.columns.Select((key, i) => new {key, i}).ToDictionary(p => Enum.Parse<Key>(p.key), p => p.i);
        Assert.IsTrue(recorded.data.Length > 0);
        Assert.IsTrue(recorded.hertz > 0.00001f);
        foreach (var line in recorded.data) {
            Assert.IsTrue(line.Length == available.Count);
            foreach (var vector in line) {
                Assert.IsTrue(vector.Length == 4);
            }
        }
        bodyPoints = recorded.data[0];
        InvokeRepeating("CallBack", 0f, 1f / recorded.hertz);
    }
    void CallBack() {
        bodyPoints = recorded.data[i];
        i = (i + 1) % recorded.data.Length;
        EmitBodyPointsUpdatedEvent();
    }


    [System.Serializable]
    private struct Recorded
    {
        public float hertz;
        public string[] columns;
        public float[][][] data;
    }
}
