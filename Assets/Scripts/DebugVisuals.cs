using UnityEngine;

public class Visual
{
    public static Color yellow = Color.Lerp(Color.yellow, Color.white, 0.5f);
    public static Color green = Color.Lerp(Color.green, Color.white, 0.5f);
    public static Color blue = Color.Lerp(Color.blue, Color.white, 0.5f);
    public static Color red = Color.Lerp(Color.red, Color.white, 0.5f);
    public static Color magenta = Color.Lerp(Color.magenta, Color.white, 0.5f);
    public static Color white = Color.Lerp(Color.white, Color.white, 0.5f);
    public class Sphere
    {
        private readonly GameObject go;

        public Sphere(Transform parent, float radius, Color color, string name = "Sphere")
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent);
            go.name = name;
            go.transform.localScale = Vector3.one * radius;
            var m = go.GetComponent<MeshRenderer>().material;
            m.SetFloat("_Glossiness", 0f);
            m.color = color;
        }

        public Vector3 At
        {
            set { go.transform.localPosition = value; }
        }

        public Color Color
        {
            set { go.GetComponent<Renderer>().material.color = value; }
        }

        public void Remove()
        {
            GameObject.Destroy(go);
        }
    }
    public class Cylinder
    {
        private readonly GameObject go;

        public Cylinder(Transform parent, float radius, Color color, string name = "Cylinder")
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent);
            go.name = name;
            go.transform.localScale = Vector3.one * radius;
            var m = go.GetComponent<MeshRenderer>().material;
            m.SetFloat("_Glossiness", 0f);
            m.color = color;
        }

        public (Vector3, Vector3) Between
        {
            set
            {
                var (pos1, pos2) = value;
                var distance = Vector3.Distance(pos1, pos2);
                go.transform.localPosition = pos1;
                go.transform.LookAt(go.transform.parent.TransformPoint(pos2));
                go.transform.Rotate(new(90f, 0f, 0f));
                go.transform.Translate(new(0f, distance / 2f, 0f));
                go.transform.localScale = new(go.transform.localScale.x, distance / 2f, go.transform.localScale.z);
            }
        }

        public (Vector3, Vector3) Toward {
            set {
                var (pos1, pos2) = value;
                Between = (pos1, 60f * (pos2 - pos1) + pos2);
            }
        }
        public Color Color
        {
            set { go.GetComponent<Renderer>().material.color = value; }
        }
        public void Remove()
        {
            GameObject.Destroy(go);
        }
    }
}
