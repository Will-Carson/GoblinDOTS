using DeBroglie.Rot;
using UnityEngine;

namespace Tessera
{
    internal static class GeometryUtils
    {
        internal static Quaternion ToQuaternion(Rotation r)
        {
            return Quaternion.Euler(0, -r.RotateCw, 0);
        }

        internal static Matrix4x4 ToMatrix(Rotation r)
        {
            var q = Quaternion.Euler(0, -r.RotateCw, 0);
            return Matrix4x4.TRS(Vector3.zero, q, new Vector3(r.ReflectX ? -1 : 1, 1, 1));
        }
    }
}
