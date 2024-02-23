using UnityEngine;

namespace MediaPipe.HandPose {
    public class HandLogger : MonoBehaviour
    {
        [SerializeField] float capturesPerSecond = 4f;
        [SerializeField] HandProvider handProvider = null;

        public void Start() {
            InvokeRepeating("CallBack", 0f, 1f / capturesPerSecond);
        }

        public void CallBack() {
            var points = handProvider.GetKeyPoints();
            var output = "";
            for (int i = 0; i < HandProvider.KeyPointCount; i++) {
                output += ((Vector3)points[i]).ToString();
            }
            Debug.Log(output);
        }

    }
}