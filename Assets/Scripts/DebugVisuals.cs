using UnityEngine;

public class DebugVisuals
{
    public static GameObject CreateSphere(float radius, Color color, string name = "Sphere") {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.localScale = Vector3.one * radius;
        var m = sphere.GetComponent<MeshRenderer>().material;
        m.SetFloat("_Glossiness", 0f);
        m.color = color;
        return sphere;
    }
    public static void SphereAt(GameObject go, Vector3 pos) {
        go.transform.position = pos;
    }

    public static GameObject CreateCylinder(float radius, Color color, string name = "Cylinder") {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.localScale = Vector3.one * radius;
        var m = cylinder.GetComponent<MeshRenderer>().material;
        m.SetFloat("_Glossiness", 0f);
        m.color = color;
        return cylinder;
    }

    public static void CylinderBetween(GameObject go, Vector3 pos1, Vector3 pos2) {
        var distance = Vector3.Distance(pos1, pos2);
        go.transform.position = pos1;
        go.transform.LookAt(pos2);
        go.transform.Rotate(new(90f, 0f, 0f));
        go.transform.Translate(new(0f, distance / 2f, 0f));
        go.transform.localScale = new(go.transform.localScale.x, distance / 2f, go.transform.localScale.z);
    }
    public static void CylinderToward(GameObject go, Vector3 pos1, Vector3 pos2) {
        CylinderBetween(go, pos1, 60f * (pos2 - pos1) + pos2);
    }
}
