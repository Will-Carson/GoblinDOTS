using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Tessera.TesseraGeneratorHelper;

namespace Tessera
{
    /// <summary>
    /// Utilities relating to ITesseraInitialConstraint
    /// </summary>
    internal class TesseraInitialConstraintHelper
    {
        private readonly TilePropagator propagator;
        private readonly IExtendedTopology extendedTopology;
        private readonly ICellType cellType;
        private readonly TileModelInfo tileModelInfo;
        private readonly TesseraPalette palette;

        // Use existing objects as initial constraint
        HashSet<(Vector3Int, Direction)> isConstrained = new HashSet<(Vector3Int, Direction)>();

        public TesseraInitialConstraintHelper(TilePropagator propagator,
            IExtendedTopology extendedTopology,
            ICellType cellType,
            TileModelInfo tileModelInfo,
            TesseraPalette palette)
        {
            this.propagator = propagator;
            this.extendedTopology = extendedTopology;
            this.cellType = cellType;
            this.tileModelInfo = tileModelInfo;
            this.palette = palette;
        }

        public void Apply(ITesseraInitialConstraint initialConstraint)
        {
            var topology = propagator.Topology;

            if (initialConstraint is TesseraInitialConstraint faceConstraint)
            {
                foreach (var (offset, faceDir, faceDetails) in faceConstraint.faceDetails)
                {
                    var rotatedOffset = faceConstraint.rotator.Multiply(offset);
                    var (rotatedDir, rotatedFaceDetails) = cellType.ApplyRotator(faceDir, faceDetails, faceConstraint.rotator);
                    FaceConstrain(faceConstraint.cell + rotatedOffset, rotatedDir, rotatedFaceDetails);
                }
            }
            else if (initialConstraint is TesseraVolumeFilter volumeFilter)
            {
                var tilesHs = new HashSet<TesseraTileBase>(volumeFilter.tiles);
                var tileSet = propagator.CreateTileSet(tileModelInfo.AllTiles
                    .Select(x => x.Item1)
                    .Where(x => tilesHs.Contains(((ModelTile)x.Value).Tile)));
                foreach (var index in topology.GetIndices())
                {
                    if (volumeFilter.mask[index])
                    {
                        topology.GetCoord(index, out var x, out var y, out var z);
                        propagator.Select(x, y, z, tileSet);
                    }
                }
            }
            else
            {
                throw new Exception($"Unexpected initial constraint type {initialConstraint.GetType()}");
            }
        }

        // Deprecate this, it depends on p being *outside* of bounds
        void FaceConstrain(Vector3Int p, Direction dir, FaceDetails faceDetails)
        {
            var p1 = p;
            if (extendedTopology.TryMove(p1, dir, out var p2, out var _))
            {
                FaceConstrain2(p2, cellType.Invert(dir), faceDetails);
            }
        }

        public void FaceConstrain2(Vector3Int p2, Direction inverseDir, FaceDetails faceDetails)
        {
            var mask = propagator.Topology.Mask;
            if (extendedTopology.InBounds(p2) && (mask == null || mask[extendedTopology.GetIndex(p2)]) && isConstrained.Add((p2, inverseDir)))
            {
                //Debug.Log(("face constraint", p2, inverseDir, faceDetails));

                var matchingTiles = tileModelInfo.TilesByDirection[inverseDir]
                    .Where(x => palette.Match(faceDetails, x.Item1))
                    .Select(x => x.Item2)
                    .ToList();

                propagator.Select(p2.x, p2.y, p2.z, matchingTiles);
            }
        }


        // Alter the initial mask
        public static bool[] GetMask(IExtendedTopology extendedTopology, IEnumerable<ITesseraInitialConstraint> initialConstraints)
        {
            var mask = extendedTopology.Topology.Mask ?? extendedTopology.Topology.GetIndices().Select(x => true).ToArray();

            // Use existing objects as mask
            foreach (var ic in initialConstraints)
            {
                if (ic is TesseraInitialConstraint faceConstraint)
                {
                    foreach (var offset in faceConstraint.offsets)
                    {
                        var p2 = faceConstraint.cell + faceConstraint.rotator.Multiply(offset);
                        if (extendedTopology.InBounds(p2))
                            mask[extendedTopology.GetIndex(p2)] = false;
                    }
                }
                else if (ic is TesseraVolumeFilter volumeFilter)
                {
                }
                else
                {
                    throw new Exception($"Unexpected initial constraint type {ic.GetType()}");
                }
            }

            return mask;
        }
    }
}
