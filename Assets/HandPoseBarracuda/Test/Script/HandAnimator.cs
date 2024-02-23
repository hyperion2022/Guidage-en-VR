using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.HandPose;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public sealed class HandAnimator : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] HandProvider handProvider = null;
    [Space]
    [SerializeField] Mesh _jointMesh = null;
    [SerializeField] Mesh _boneMesh = null;
    [Space]
    [SerializeField] Material _jointMaterial = null;
    [SerializeField] Material _boneMaterial = null;

    #endregion

    #region Private members


    static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),     // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 17), (2, 5), (5, 9), (9, 13), (13, 17)  // Palm
    };

    Matrix4x4 CalculateJointXform(Vector3 pos)
      => Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.07f);

    Matrix4x4 CalculateBoneXform(Vector3 p1, Vector3 p2)
    {
        var length = Vector3.Distance(p1, p2) / 2;
        var radius = 0.03f;

        var center = (p1 + p2) / 2;
        var rotation = Quaternion.FromToRotation(Vector3.up, p2 - p1);
        var scale = new Vector3(radius, length, radius);

        return Matrix4x4.TRS(center, rotation, scale);
    }

    #endregion

    #region MonoBehaviour implementation


    void LateUpdate()
    {
        var layer = gameObject.layer;

        // Joint balls
        for (var i = 0; i < HandProvider.KeyPointCount; i++)
        {
            var xform = CalculateJointXform(handProvider.GetKeyPoint(i));
            Graphics.DrawMesh(_jointMesh, xform, _jointMaterial, layer);
        }

        // Bones
        foreach (var pair in BonePairs)
        {
            var p1 = handProvider.GetKeyPoint(pair.Item1);
            var p2 = handProvider.GetKeyPoint(pair.Item2);
            var xform = CalculateBoneXform(p1, p2);
            Graphics.DrawMesh(_boneMesh, xform, _boneMaterial, layer);
        }
    }

    #endregion
}
