using UnityEngine;

public class InspectBaracudaInput : MonoBehaviour
{
    [SerializeField] ComputeShader shader;
    public ComputeBuffer source = null;
    private RenderTexture texture;
    void Start()
    {
        texture = new RenderTexture(224, 224, 0) { enableRandomWrite = true };
        GetComponent<MeshRenderer>().material.mainTexture = texture;
    }

    void Update()
    {
        if (source != null)
        {
            shader.SetBuffer(0, "input", source);
            shader.SetTexture(0, "output", texture);
            shader.Dispatch(0, 224/8, 224/8, 1);
        }
    }
}
