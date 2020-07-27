using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    internal class MeshExtendedTopology : IExtendedTopology
    {
        private readonly Transform transform;
        private readonly ICellType cellType;
        private readonly Mesh surfaceMesh;
        private readonly int layerCount;
        private int[] faceCounts;
        private int maxFaceCount;
        private int subMeshCount;
        private readonly float tileHeight;
        private readonly float surfaceOffset;
        private readonly GraphInfo info;
        private readonly SubMeshTopology topology;
        private readonly bool surfaceSmoothNormals;

        public MeshExtendedTopology(Transform transform, ICellType cellType, Mesh surfaceMesh, int layerCount, float tileHeight, float surfaceOffset, bool surfaceSmoothNormals, GraphInfo info, SubMeshTopology topology)
        {
            if (!(cellType is CubeCellType))
            {
                throw new NotImplementedException();
            }
            this.transform = transform;
            this.cellType = cellType;
            this.surfaceMesh = surfaceMesh;
            this.layerCount = layerCount;
            this.tileHeight = tileHeight;
            this.surfaceOffset = surfaceOffset;
            this.surfaceSmoothNormals = surfaceSmoothNormals;
            this.info = info;
            this.topology = topology;
            this.faceCounts =  Enumerable.Range(0, surfaceMesh.subMeshCount).Select(i => (int)surfaceMesh.GetIndexCount(i) / 4).ToArray();
            this.maxFaceCount = faceCounts.Max();
            this.subMeshCount = surfaceMesh.subMeshCount;
        }

        public int IndexCount => topology.IndexCount;

        public ICellType CellType => cellType;

        public ITopology Topology => topology;

        public Vector3Int GetCell(int index)
        {
            return new Vector3Int(index % maxFaceCount, (index / maxFaceCount) % layerCount, index / maxFaceCount / layerCount);
        }

        public bool GetCell(TesseraTile tile, Matrix4x4 tileLocalToWorldMatrix, out Vector3Int cell, out MatrixInt3x3 rotator)
        {
            // TODO: Implement
            cell = new Vector3Int();
            rotator = new MatrixInt3x3();
            return false;
        }

        public Vector3 GetCellCenter(Vector3Int cell)
        {
            var meshDeformation = MeshUtils.GetDeformation(surfaceMesh, tileHeight, surfaceOffset, surfaceSmoothNormals, cell.x, cell.y, cell.z);
            return meshDeformation.DeformPoint(Vector3.zero);
        }

        public TRS GetTRS(Vector3Int cell)
        {
            var meshDeformation = MeshUtils.GetDeformation(surfaceMesh, tileHeight, surfaceOffset, surfaceSmoothNormals, cell.x, cell.y, cell.z);
            var center = meshDeformation.DeformPoint(Vector3.zero);
            var e = 1e-4f;
            var x = (meshDeformation.DeformPoint(Vector3.right * e) - center) / e;
            var y = (meshDeformation.DeformPoint(Vector3.up * e) - center) / e;
            var z = (meshDeformation.DeformPoint(Vector3.forward * e) - center) / e;
            var m = new Matrix4x4(x, y, z, new Vector4(center.x, center.y, center.z, 1));
            var x2 = m.MultiplyPoint3x4(Vector3.right);
            var y2 = m.MultiplyPoint3x4(Vector3.up);
            var z2 = m.MultiplyPoint3x4(Vector3.forward);
            var m2 = new Matrix4x4();

            m2.m00 = 0.1514f;m2.m01 = 0.00000f;m2.m02 = -1.19269f;m2.m03 = 0.59614f;
            m2.m10 = 0.00000f; m2.m11 = 1.00017f; m2.m12 = 0.00000f; m2.m13 = 0.50000f;
            m2.m20 = -1.54018f; m2.m21 = 0.00000f; m2.m22 = -0.11444f; m2.m23 = -6.05271f;
            m2.m30 = 0.00000f; m2.m31 = 0.00000f; m2.m32 = 0.00000f; m2.m33 = 1.0000f;

            //var p = m2.MultiplyPoint3x4(Vector3.zero);
            //var r = m2.rotation


            return new TRS(m);
        }

        public int GetIndex(Vector3Int cell)
        {
            return cell.x + cell.y * maxFaceCount + cell.z * maxFaceCount * layerCount;
        }

        public bool InBounds(Vector3Int cell)
        {
            return 
                0 <= cell.y && cell.y < layerCount && 
                0 <= cell.z && cell.z < subMeshCount &&
                0 <= cell.x && cell.x < faceCounts[cell.z];
        }

        public bool TryMove(Vector3Int cell, Direction d, out Vector3Int dest, out Rotation rotation)
        {
            if(!topology.TryMove(GetIndex(cell), d, out var destIndex, out var inverseDirection, out var edgeLabel))
            {
                dest = new Vector3Int();
                rotation = new Rotation();
                return false;
            }

            dest = GetCell(destIndex);
            rotation = info.EdgeLabelInfo[(int)edgeLabel].Item3;
            return true;
        }
    }

}