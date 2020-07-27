using System;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Encapsulates an arbitrary deformation of mesh vertices
    /// </summary>
    public class MeshDeformation
    {
        public MeshDeformation(Func<Vector3, Vector3> deformPoint, Func<Vector3, Vector3, Vector3> deformNormal, Func<Vector3, Vector4, Vector4> deformTangent, bool invertWinding)
        {
            InnerDeformPoint = deformPoint;
            InnerDeformNormal = deformNormal;
            InnerDeformTangent = deformTangent;
            InnerInvertWinding = invertWinding;
        }

        public Func<Vector3, Vector3> InnerDeformPoint { get; set; }
        public Func<Vector3, Vector3, Vector3> InnerDeformNormal { get; set; }
        public Func<Vector3, Vector4, Vector4> InnerDeformTangent { get; set; }
        public bool InnerInvertWinding { get; set; }

        public Matrix4x4 PreDeform { get; private set; } = Matrix4x4.identity;
        public Matrix4x4 PostDeform { get; private set; } = Matrix4x4.identity;
        public Matrix4x4 PreDeformIT { get; private set; } = Matrix4x4.identity;
        public Matrix4x4 PostDeformIT { get; private set; } = Matrix4x4.identity;

        public bool InvertWinding => InnerInvertWinding ^ (PreDeform.determinant < 0) ^ (PostDeform.determinant < 0) ^ true;

        public MeshDeformation Clone()
        {
            return (MeshDeformation)MemberwiseClone();
        }

        public Vector3 DeformPoint(Vector3 p)
        {
            return PostDeform.MultiplyPoint3x4(InnerDeformPoint(PreDeform.MultiplyPoint3x4(p)));
        }

        public Vector3 DeformNormal(Vector3 p, Vector3 v)
        {
            return PostDeformIT.MultiplyVector(InnerDeformNormal(PreDeform.MultiplyPoint3x4(p), PreDeformIT.MultiplyVector(v)));
        }

        private Vector4 DeformTangent(Vector3 p, Vector4 t)
        {
            Vector3 t2 = PreDeform.MultiplyVector(t);
            Vector4 t3 = new Vector4(t2.x, t2.y, t2.z, t.w);
            Vector4 t4 = InnerDeformTangent(PreDeform.MultiplyPoint3x4(p), t3);
            Vector3 t5 = PostDeform.MultiplyVector(t4);
            return new Vector4(t5.x, t5.y, t5.z, t4.w);
        }

        private Mesh Deform(Mesh mesh, int submeshStart, int submeshCount)
        {
            var newMesh = new Mesh();
            newMesh.subMeshCount = submeshCount;

            // Copy deformed data
            newMesh.vertices = mesh.vertices.Select(DeformPoint).ToArray();
            newMesh.normals = mesh.vertices.Zip(mesh.normals, (a, b) => DeformNormal(a, b)).ToArray();
            newMesh.tangents = mesh.vertices.Zip(mesh.tangents, DeformTangent).ToArray();

            // Copy untransformed data
            newMesh.uv = mesh.uv;
            newMesh.uv2 = mesh.uv2;
            newMesh.uv3 = mesh.uv3;
            newMesh.uv4 = mesh.uv4;
            newMesh.uv5 = mesh.uv5;
            newMesh.uv6 = mesh.uv6;
            newMesh.uv7 = mesh.uv7;
            newMesh.uv8 = mesh.uv8;
            newMesh.colors = mesh.colors;
            newMesh.colors32 = mesh.colors32;

            // Copy indices
            for (var i = 0; i < submeshCount; i++)
            {
                var indices = mesh.GetIndices(submeshStart + i, false);
                indices = InvertWinding ? indices.Reverse().ToArray() : indices;
                newMesh.SetIndices(indices, mesh.GetTopology(submeshStart + i), i, true, (int)mesh.GetBaseVertex(submeshStart + i));
            }

            newMesh.name = mesh.name + "(Clone)";

            return newMesh;
        }

        /// <summary>
        /// Deforms the vertices and normals of a mesh as specified.
        /// </summary>
        public Mesh Deform(Mesh mesh)
        {
            return Deform(mesh, 0, mesh.subMeshCount);
        }

        /// <summary>
        /// Transforms the vertices and normals of a submesh mesh as specified.
        /// </summary>
        public Mesh Transform(Mesh mesh, int submesh)
        {
            return Deform(mesh, submesh, 1);
        }

        public static MeshDeformation operator *(MeshDeformation meshDeformation, Matrix4x4 m)
        {
            var r = meshDeformation.Clone();
            r.PreDeform = r.PreDeform * m;
            r.PreDeformIT = r.PreDeformIT * m.inverse.transpose;
            return r;
        }

        public static MeshDeformation operator *(Matrix4x4 m, MeshDeformation meshDeformation)
        {
            var r = meshDeformation.Clone();
            r.PostDeform = m * r.PostDeform;
            r.PostDeformIT = m.inverse.transpose * r.PostDeformIT;
            return r;
        }
    }
}
