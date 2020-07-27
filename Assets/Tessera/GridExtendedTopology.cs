using DeBroglie.Rot;
using DeBroglie.Topo;
using UnityEngine;

namespace Tessera
{
    internal class GridExtendedTopology : IExtendedTopology
    {
        private readonly Transform transform;
        private readonly ICellType cellType;
        private readonly Vector3 center;
        private readonly Vector3Int size;
        private readonly Vector3 tileSize;

        private GridTopology topology;

        public GridExtendedTopology(Transform transform, ICellType cellType, Vector3 center, Vector3Int size, Vector3 tileSize)
        {
            this.transform = transform;
            this.cellType = cellType;
            this.center = center;
            this.size = size;
            this.tileSize = tileSize;

            this.topology = new GridTopology(size.x, size.y, size.z, false);
        }

        public int IndexCount => topology.IndexCount;

        public ICellType CellType => cellType;

        public ITopology Topology => topology;

        public Vector3Int GetCell(int index)
        {
            topology.GetCoord(index, out var x, out var y, out var z);
            return new Vector3Int(x, y, z);
        }

        public int GetIndex(Vector3Int cell)
        {
            return topology.GetIndex(cell.x, cell.y, cell.z);
        }

        public bool InBounds(Vector3Int cell)
        {
            return CubeGeometryUtils.InBounds(cell, size);
        }

        public Vector3 GetCellCenter(Vector3Int cell)
        {
            var min = center - Vector3.Scale(size - Vector3Int.one, tileSize) / 2.0f;
            return min + Vector3.Scale(tileSize, cell);
        }

        public TRS GetTRS(Vector3Int cell)
        {
            return new TRS(GetCellCenter(cell));
        }


        public bool GetCell(
            TesseraTile tile,
            Matrix4x4 tileLocalToWorldMatrix,
            out Vector3Int cell,
            out MatrixInt3x3 rotator)
        {
            return CubeGeometryUtils.GetCell(
                transform,
                center,
                tileSize,
                size,
                tile, 
                tileLocalToWorldMatrix,
                out cell,
                out rotator);
        }

        public ITopology WithMask(bool[] mask)
        {
            return topology.WithMask(mask);
        }

        public bool TryMove(Vector3Int cell, Direction d, out Vector3Int dest, out Rotation rotation)
        {
            rotation = new Rotation();
            switch(d)
            {
                case Direction.XPlus: cell.x += 1; break;
                case Direction.XMinus: cell.x -= 1; break;
                case Direction.YPlus: cell.y += 1; break;
                case Direction.YMinus: cell.y -= 1; break;
                case Direction.ZPlus: cell.z += 1; break;
                case Direction.ZMinus: cell.z -= 1; break;
            }
            dest = cell;
            return true;
        }


    }
}