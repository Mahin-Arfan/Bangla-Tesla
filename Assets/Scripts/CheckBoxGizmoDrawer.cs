using UnityEngine;
using System.Collections.Generic;

public class CheckBoxGizmoDrawer : MonoBehaviour
{
    public static List<CheckBoxData> boxes = new List<CheckBoxData>();

    public struct CheckBoxData
    {
        public Vector3 center;
        public Vector3 halfExtents;
        public Quaternion rotation;

        public CheckBoxData(Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.rotation = rotation;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var box in boxes)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(box.center, box.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.zero, box.halfExtents * 2f);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
