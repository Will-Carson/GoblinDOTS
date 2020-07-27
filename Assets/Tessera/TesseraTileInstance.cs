using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeBroglie.Rot;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Represents a request to instantiate a TesseraTile, post generation.
    /// </summary>
    public class TesseraTileInstance
    {
        public TesseraTile Tile { get; internal set; }
        // TRS in World space
        public Vector3 Position { get; internal set; }
        public Quaternion Rotation { get; internal set; }
        public Vector3 LossyScale { get; internal set; }
        // TRS in generator space
        public Vector3 LocalPosition { get; internal set; }
        public Quaternion LocalRotation { get; internal set; }
        public Vector3 LocalScale { get; internal set; }

        // Rotation and scale just from the tile, not from the position of the cell
        public Quaternion TileRotation { get; internal set; }
        public Vector3 TileScale { get; internal set; }

        public Vector3Int Cell { get; internal set; }

        public Vector3Int[] Cells { get; internal set; }

        // From tile space to generator space
        public MeshDeformation MeshDeformation { get;  internal set; }
    }
}
