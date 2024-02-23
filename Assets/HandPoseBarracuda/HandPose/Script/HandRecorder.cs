using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MediaPipe.HandPose {
    public class HandRecorder : MonoBehaviour
    {
        [SerializeField] float capturesPerSecond = 4f;
        [SerializeField] HandProvider handProvider = null;
        [SerializeField] string outputFilePath = "recorded-hand.json";

        List<HandProvider.Hand> recorded;
        public void Start() {
            recorded = new List<HandProvider.Hand>();
            InvokeRepeating("CallBack", 0f, 1f / capturesPerSecond);
        }

        public void CallBack() {
            recorded.Add(new HandProvider.Hand(){
                points = handProvider.GetKeyPoints().ToArray()
            });
        }

        public void OnDestroy() {
            File.WriteAllText(outputFilePath, JsonUtility.ToJson(new HandProvider.Recorded(){
                rate = capturesPerSecond,
                recs = recorded.ToArray()
            }));
        }
    }
}