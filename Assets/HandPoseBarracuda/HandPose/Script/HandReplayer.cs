using System.IO;
using UnityEngine;

namespace MediaPipe.HandPose {
    public class HandReplayer : HandProvider
    {
        [SerializeField] string inputFilePath = "recorded-hand.json";

        Recorded recorded;
        Hand hand;
        int i = 0;

        public override Vector4[] GetKeyPoints()
            => hand.points;

        // Start is called before the first frame update
        void Start()
        {
            recorded = JsonUtility.FromJson<Recorded>(File.ReadAllText(inputFilePath));
            InvokeRepeating("CallBack", 0f, 1f / recorded.rate);
        }
        
        void CallBack() {
            hand = recorded.recs[i % recorded.recs.Length];
            i += 1;
        }
    }
}