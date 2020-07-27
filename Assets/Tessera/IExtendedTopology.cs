using DeBroglie.Rot;
using DeBroglie.Topo;
using UnityEngine;

namespace Tessera
{
    // TODO: Refactor so we don't need this separate interface
    internal interface IExtendedTopologyLite
    {
        int IndexCount { get; }

        ICellType CellType { get; }

        int GetIndex(Vector3Int cell);

        Vector3Int GetCell(int index);

        /// <summary>
        /// Returns the center of the cell in local space
        /// </summary>
        Vector3 GetCellCenter(Vector3Int cell);

        TRS GetTRS(Vector3Int cell);

        /// <summary>
        /// Returns true if the cell is actually in the topology. Ignores masking.
        /// </summary>
        bool InBounds(Vector3Int cell);

        /// <summary>
        /// Returns the cell, and a rotator for a tile placed near the generator
        /// A rotator takes vectors in the tile local space, and returns them in generator local space.
        /// It's always an distance presering transform (so it's always one of the symmetries of a cube).
        /// 
        /// NB: The cell returned corresponds to offset (0,0,0). The tile may not actually occupy that offset.
        /// </summary>
        bool GetCell(
            TesseraTile tile,
            Matrix4x4 tileLocalToWorldMatrix,
            out Vector3Int cell,
            out MatrixInt3x3 rotator);
    }


    // Functions similarly to DeBroglie.Topo.ITopology
    // but also manages conversions from world space to topology indices.
    internal interface IExtendedTopology : IExtendedTopologyLite
    {

        ITopology Topology { get; }

        bool TryMove(Vector3Int cell, Direction d, out Vector3Int dest, out Rotation rotation);
    }
}