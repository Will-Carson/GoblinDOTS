using UnityEngine;

namespace Tessera
{
    internal class TRS
    {
        public TRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public TRS(Vector3 position)
        {
            Position = position;
            Rotation = Quaternion.identity;
            Scale = Vector3.one;
        }

        public TRS(Matrix4x4 m)
        {
            var scale = m.lossyScale;
            m = m * Matrix4x4.Scale(new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z));
            Position = m.MultiplyPoint(Vector3.zero);
            Rotation = m.rotation;
            Scale = scale;
        }

        public TRS(Transform t)
        {
            Position = t.position;
            Rotation = t.rotation;
            Scale = t.lossyScale;
        }

        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.TRS(Position, Rotation, Scale);
        }

        public static TRS operator*(TRS a, TRS b)
        {
            // TOOD: More efficient
            return new TRS(a.ToMatrix() * b.ToMatrix());
        }

        public Vector3 Position { get; internal set; }
        public Quaternion Rotation { get; internal set; }
        public Vector3 Scale { get; internal set; }
    }
}
