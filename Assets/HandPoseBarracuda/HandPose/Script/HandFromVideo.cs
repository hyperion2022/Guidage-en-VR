using Klak.TestTools;
using UnityEngine;

namespace MediaPipe.HandPose {
    public class HandFromVideo : HandProvider
    {
        [SerializeField] ImageSource _source = null;
        [Space]
        [SerializeField] ResourceSet _resources = null;

        HandPipeline _pipeline;

        public override Vector4[] GetKeyPoints()
            => _pipeline.GetKeyPoints();

        void Start()
        {
            _pipeline = new HandPipeline(_resources);
        }
        
        void OnDestroy() {
            _pipeline.Dispose();
        }
        
        void LateUpdate()
        {
            _pipeline.ProcessImage(_source.Texture);
        }
    }
}