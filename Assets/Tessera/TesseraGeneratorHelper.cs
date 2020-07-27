using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using static Tessera.GeometryUtils;

namespace Tessera
{
    /**
     * Holds the actual implementation of the genreation
     * In a format that is appropriate to run in threads.
     * I.e. all relevant GameObject properties have been copied into POCOs
     */
    internal class TesseraGeneratorHelper
    {
        // Configuration.
        // This is all loaded from TesseraGenerator
        TesseraPalette palette;
        TileModel model;
        TileModelInfo tileModelInfo;
        List<ITesseraInitialConstraint> initialConstraints;
        List<ITileConstraint> constraints;
        IExtendedTopology extendedTopology;
        bool backtrack;
        TesseraInitialConstraint skyBox;
        Action<string, float> progress;
        Action<ITopoArray<ISet<ModelTile>>> progressTiles;
        XoRoRNG xororng;
        CancellationToken ct;

        // State. This is intialized in Setup()
        DeBroglie.Resolution lastStatus;
        TilePropagator propagator;

        // TODO: Record some timings here

        public TesseraGeneratorHelper(
            TesseraPalette palette,
            TileModel model,
            TileModelInfo tileModelInfo,
            List<ITesseraInitialConstraint> initialConstraints,
            List<ITileConstraint> constraints,
            IExtendedTopology extendedTopology,
            bool backtrack,
            TesseraInitialConstraint skyBox,
            Action<string, float> progress,
            Action<ITopoArray<ISet<ModelTile>>> progressTiles,
            XoRoRNG xororng,
            CancellationToken ct)
        {
            this.palette = palette;
            this.model = model;
            this.tileModelInfo = tileModelInfo;
            this.initialConstraints = initialConstraints;
            this.constraints = constraints;
            this.extendedTopology = extendedTopology;
            this.backtrack = backtrack;
            this.skyBox = skyBox;
            this.progress = progress;
            this.progressTiles = progressTiles;
            this.xororng = xororng;
            this.ct = ct;
        }

        public TilePropagator Propagator => propagator;

        public IExtendedTopology ExtendedTopology => extendedTopology;

        public void SetupAndRun()
        {
            Setup();
            Run();
        }


        // Construct the propagator
        internal void Setup()
        {
            progress?.Invoke("Initializing", 0.0f);

            var topology = GetTopology();

            var options = new TilePropagatorOptions
            {
                BackTrackDepth = backtrack ? -1 : 0,
                RandomDouble = xororng.NextDouble,
                Constraints = constraints.ToArray(),
            };
            propagator = new TilePropagator(model, topology, options);
            lastStatus = DeBroglie.Resolution.Undecided;

            CheckStatus("Failed to initialize propagator");

            ApplyInitialConstraintsAndSkybox();
            BanBigTiles();
        }

        ITopology GetTopology()
        {
            var mask = TesseraInitialConstraintHelper.GetMask(extendedTopology, initialConstraints);
            return extendedTopology.Topology.WithMask(mask);
        }


        private void Run()
        {
            CheckStatus("Propagator is not ready to run");


            {
                var lastProgress = DateTime.Now;
                var progressResolution = TimeSpan.FromSeconds(0.1);
                while (propagator.Status == DeBroglie.Resolution.Undecided)
                {
                    ct.ThrowIfCancellationRequested();
                    if (lastProgress + progressResolution < DateTime.Now)
                    {
                        lastProgress = DateTime.Now;
                        if (progress != null)
                        {
                            progress("Generating", (float)propagator.GetProgress());
                        }
                        if (progressTiles != null)
                        {
                            progressTiles(propagator.ToValueSets<ModelTile>());
                        }
                    }
                    propagator.Step();
                }
            }
        }


        private void ApplyInitialConstraintsAndSkybox()
        {
            var cellType = new CubeCellType();

            var et = extendedTopology;
            var topology = propagator.Topology;
            var mask = topology.Mask;

            var initialConstraintHelper = new TesseraInitialConstraintHelper(propagator, extendedTopology, cellType, tileModelInfo, palette);

            foreach (var ic in initialConstraints)
            {
                initialConstraintHelper.Apply(ic);
                CheckStatus($"Contradiction after setting initial constraint {ic.Name}.");
            }
            CheckStatus("Contradiction after setting initial constraints.");

            // Apply skybox (if any)
            if (skyBox != null)
            {
                var skyBoxFaceDetailsByDir = Enumerable.Range(0, topology.DirectionsCount).ToDictionary(x => x, x => skyBox.faceDetails.First(f => f.faceDir == (FaceDir)x).faceDetails);
                foreach(var index in topology.GetIndices())
                {
                    for (var d= 0;d< topology.DirectionsCount;d++)
                    {
                        if(!et.Topology.TryMove(index, (Direction)d, out var _))
                        {
                            // Edge of topology (unmaked)
                            initialConstraintHelper.FaceConstrain2(et.GetCell(index), (Direction)d, skyBoxFaceDetailsByDir[d]);
                        }
                    }
                }
            }
            CheckStatus("Contradiction after setting initial constraints and skybox.");
        }

        private IEnumerable<(Vector3Int, Direction)> GetBorders()
        {
            var topology = ExtendedTopology.Topology;
            // TODO: Could special case for regular grids
            foreach (var index in topology.GetIndices())
            {
                for (var d = 0; d < topology.DirectionsCount; d++)
                {
                    if (!extendedTopology.Topology.TryMove(index, (Direction)d, out var _))
                    {
                        topology.GetCoord(index, out var x, out var y, out var z);
                        yield return (new Vector3Int(x, y, z), (Direction)d);
                    }
                }
            }
        }

        // As big tiles are split into smaller tiles before sending to DeBroglie, 
        // the generation can make big tiles that overhang the boundary of the generation area.
        // This bans such setups.
        private void BanBigTiles()
        {
            var internalAdjacenciesByDirection = tileModelInfo.InternalAdjacencies
                .ToLookup(t => t.Item3, t => t.Item1);
            foreach (var (p, dir) in GetBorders())
            {
                foreach (var t in internalAdjacenciesByDirection[dir])
                {
                    propagator.Ban(p.x, p.y, p.z, t);
                }
            }
            CheckStatus("Contradiction after removing big tiles overlapping edges.");
        }

        // TODO: This should return via TesseraCompletion rather than logging
        private void CheckStatus(string s)
        {
            if (lastStatus != DeBroglie.Resolution.Contradiction && propagator.Status == DeBroglie.Resolution.Contradiction)
            {
                lastStatus = propagator.Status;
                Debug.LogWarning(s);
            }
        }

        internal class TileModelInfo
        {
            public List<(Tile, float)> AllTiles { get; set; }
            public List<(Tile, Tile, Direction)> InternalAdjacencies { get; set; }
            public Dictionary<Direction, List<(FaceDetails, Tile)>> TilesByDirection { get; set; }
        }

        /// <summary>
        /// Summarizes the tiles, in preparation for building a model.
        /// </summary>
        internal static TileModelInfo GetTileModelInfo(List<TileEntry> tiles)
        {
            ICellType cellType = new CubeCellType();

            var allTiles = new List<(Tile, float)>();
            var internalAdjacencies = new List<(Tile, Tile, Direction)>();

            var tilesByDirection = cellType.GetDirections().ToDictionary(d => d, d => new List<(FaceDetails, Tile)>());

            var tileCosts = new Dictionary<TesseraTile, int>();

            var rg = cellType.GetRotationGroup();

            if (tiles == null || tiles.Count == 0)
            {
                throw new Exception("Cannot run generator with zero tiles configured.");
            }

            // Generate all tiles, and extract their face details
            foreach (var tileEntry in tiles)
            {
                var tile = tileEntry.tile;

                if (tile == null)
                    continue;
                if (!IsContiguous(tile))
                {
                    Debug.LogWarning($"Cannot use {tile} as it is not contiguous");
                    continue;
                }

                foreach (var rot in rg)
                {

                    if (!tile.rotatable && rot.RotateCw != 0)
                        continue;
                    if (!tile.reflectable && rot.ReflectX)
                        continue;

                    // Set up internal connections
                    foreach (var offset in tile.offsets)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        allTiles.Add((modelTile, tileEntry.weight / tile.offsets.Count));

                        foreach (var faceDir in cellType.GetFaceDirs())
                        {
                            var offset2 = offset + faceDir.Forward();
                            if (tile.offsets.Contains(offset2))
                            {
                                var modelTile2 = new Tile(new ModelTile(tile, rot, offset2));

                                var dir = cellType.Rotate(faceDir, rot);

                                internalAdjacencies.Add((modelTile, modelTile2, dir));
                            }
                        }
                    }

                    // Set up external connections
                    foreach (var (offset, faceDir, faceDetails) in tile.faceDetails)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        var (dir, rFaceDetails) = cellType.RotateBy(faceDir, faceDetails, rot);
                        tilesByDirection[dir].Add((rFaceDetails, modelTile));
                    }
                }
            }

            return new TileModelInfo
            {
                AllTiles = allTiles,
                InternalAdjacencies = internalAdjacencies,
                TilesByDirection = tilesByDirection,
            };
        }

        private static bool IsContiguous(TesseraTile tile)
        {
            if (tile.offsets.Count == 1)
                return true;

            // Floodfill offset
            var offsets = new HashSet<Vector3Int>(tile.offsets);
            var toRemove = new Stack<Vector3Int>();
            toRemove.Push(offsets.First());
            while (toRemove.Count > 0)
            {
                var o = toRemove.Pop();
                offsets.Remove(o);

                foreach (FaceDir faceDir in Enum.GetValues(typeof(FaceDir)))
                {
                    var o2 = o + faceDir.Forward();
                    if (offsets.Contains(o2))
                    {
                        toRemove.Push(o2);
                    }
                }
            }

            return offsets.Count == 0;
        }
    }
}
