using UnityEngine;

public class DebugVisuals
{
    public static void AddSphere(Vector3 pos, float radius, Color color, string name = "Sphere") {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * radius;
        sphere.GetComponent<MeshRenderer>().material.color = color;
    }
    public static void AddCylinder(Vector3 pos1, Vector3 pos2, float radius, Color color, string name = "Cylinder") {
        var distance = Vector3.Distance(pos1, pos2);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sphere.name = name;
        sphere.transform.position = pos1;
        sphere.transform.LookAt(pos2);
        sphere.transform.Rotate(new(90f, 0f, 0f));
        sphere.transform.Translate(new(0f, distance / 2f, 0f));
        sphere.transform.localScale = new(radius, distance / 2f, radius);
        sphere.GetComponent<MeshRenderer>().material.color = color;
    }
    public static void AddCylinderToward(Vector3 pos1, Vector3 pos2, float radius, Color color, string name = "Cylinder") {
        // it extends the cylinder far behound pos2 (60 times further)
        AddCylinder(pos1, 60f * (pos2 - pos1) + pos2, radius, color, name);
    }
}
