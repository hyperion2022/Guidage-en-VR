using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace UserOnboarding
{
    public class BodyPointsReplayer : BodyPointsProvider
    {
        [SerializeField] string inputFilePath = "recorded-body-points.json";


        Recorded recorded;
        int i = 0;
        float[][] bodyPoints;
        Dictionary<BodyPoint, int> available;

        public override BodyPoint[] ProvidedPoints => available.Keys.ToArray();
        public override (PointState, Vector3) GetBodyPoint(BodyPoint key)
        {
            var point = bodyPoints[available[key]];
            return (point[3] switch
            {
                0f => PointState.NotProvided,
                1f => PointState.Tracked,
                2f => PointState.Inferred,
                3f => PointState.NotTracked,
                _ => PointState.NotTracked,
            }, new(point[0], point[1], point[2]));
        }

        void Awake()
        {
            recorded = JsonConvert.DeserializeObject<Recorded>(File.ReadAllText(inputFilePath));
            available = recorded.columns.Select((key, i) => (key, i)).ToDictionary(p => Enum.Parse<BodyPoint>(p.key), p => p.i);
            Assert.IsTrue(recorded.data.Length > 0);
            Assert.IsTrue(recorded.hertz > 0.00001f);
            foreach (var line in recorded.data)
            {
                Assert.IsTrue(line.Length == available.Count);
                foreach (var vector in line)
                {
                    Assert.IsTrue(vector.Length == 4);
                }
            }
            bodyPoints = recorded.data[0];
        }

        void Start()
        {
            InvokeRepeating("CallBack", 0f, 1f / recorded.hertz);
        }
        void CallBack()
        {
            bodyPoints = recorded.data[i];
            i = (i + 1) % recorded.data.Length;
            RaiseBodyPointsChanged();
        }

        [Serializable]
        private struct Recorded
        {
            public float hertz;
            public string[] columns;
            public float[][][] data;
        }
    }
}
