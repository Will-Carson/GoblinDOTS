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

namespace Tessera
{
    [Serializable]
    public class TileList
    {
        public List<TesseraTileBase> tiles;
    }


    /// <summary>
    /// GameObjects with this behaviour contain utilities to generate tile based levels using Wave Function Collapse (WFC).
    /// Call <see cref="Generate"/> or <see cref="StartGenerate"/> to run.
    /// The generation takes the following steps:
    /// * Inspect the tiles in <see cref="tiles"/> and work out how they rotate and connect to each other.
    /// * Setup any initial constraints that fix parts of the generation (<see cref="searchInitialConstraints"/> and <see cref="initialConstraints"/>).
    /// * Fix the boundary of the generation if <see cref="skyBox"/> is set.
    /// * Generate a set of tile instances that fits the above tiles and constraints.
    /// * Optionally <see cref="retries"/> or <see cref="backtrack"/>.
    /// * Instantiates the tile instances.
    /// </summary>
    [AddComponentMenu("Tessera/Tessera Generator")]
    public class TesseraGenerator : MonoBehaviour
    {


        [SerializeField]
        private Vector3Int m_size = new Vector3Int(10, 1, 10);

        [SerializeField]
        private Vector3 m_center = Vector3.zero;

        /// <summary>
        /// The size of the generator area, counting in cells each of size <see cref="tileSize"/>.
        /// </summary>
        public Vector3Int size
        {
            get { return m_size; }
            set
            {
                m_size = value;
            }
        }

        /// <summary>
        /// The local position of the center of the area to generate.
        /// </summary>
        public Vector3 center
        {
            get
            {
                return m_center;
            }
            set
            {
                m_center = value;
            }
        }

        /// <summary>
        /// The area of generation.
        /// Setting this will cause the size to be rounded to a multiple of <see cref="tileSize"/>
        /// </summary>
        public Bounds bounds
        {
            get
            {
                return new Bounds(m_center, Vector3.Scale(tileSize, m_size));
            }
            set
            {
                m_center = value.center;
                m_size = new Vector3Int(
                    Math.Max(1, (int)Math.Round(value.size.x / tileSize.x)),
                    Math.Max(1, (int)Math.Round(value.size.y / tileSize.y)),
                    Math.Max(1, (int)Math.Round(value.size.z / tileSize.z))
                    );
            }
        }

        /// <summary>
        /// The list of tiles eligable for generation.
        /// </summary>
        public List<TileEntry> tiles = new List<TileEntry>();

        /// <summary>
        /// The stride between each cell in the generation.
        /// "big" tiles may occupy a multiple of this tile size.
        /// </summary>
        public Vector3 tileSize = Vector3.one;

        /// <summary>
        /// If set, backtracking will be used during generation.
        /// Backtracking can find solutions that would otherwise be failures,
        /// but can take a long time.
        /// </summary>
        public bool backtrack = false;

        /// <summary>
        /// If backtracking is off, how many times to retry generation if a solution
        /// cannot be found.
        /// </summary>
        public int retries = 5;

        /// <summary>
        /// If set, this tile is used to define extra initial constraints for the boundary.
        /// </summary>
        public TesseraTile skyBox = null;

        /// <summary>
        /// If true, then active tiles in the scene will be taken as initial constraints.
        /// If false, then <see cref="initialConstraints"/> is used instead.
        /// </summary>
        public bool searchInitialConstraints = true;

        /// <summary>
        /// The initial constraints to be used, if <see cref="searchInitialConstraints"/> is false.
        /// This can be filled with objects returned from the GetInitialConstraint methods.
        /// You are recommended to use <see cref="Tessera.TesseraGeneratorOptions.initialConstraints"/> instead.
        /// </summary>
        [Obsolete("Use TesseraGeneratorOptions.initialConstraints instead")]
        public List<TesseraInitialConstraint> initialConstraints = null;

        /// <summary>
        /// Inherited from the first tile in <see cref="tiles"/>.
        /// </summary>
        public TesseraPalette palette => tiles.FirstOrDefault()?.tile?.palette ?? TesseraPalette.defaultPalette;

        /// <summary>
        /// If set, then tiles are generated on the surface of this mesh instead of a regular grid.
        /// </summary>
        public Mesh surfaceMesh;

        /// <summary>
        /// Height above the surface mesh that the bottom layer of tiles is generated at.
        /// </summary>
        public float surfaceOffset;

        /// <summary>
        /// Controls how normals are treated for meshes deformed to fit the surfaceMesh.
        /// </summary>
        public bool surfaceSmoothNormals;

        /// <summary>
        /// If true, and a <see cref="surfaceMesh"/> is set with multiple submeshes (materials),
        /// then use surfaceSubmeshTiles.
        /// </summary>
        public bool filterSurfaceSubmeshTiles;

        /// <summary>
        /// A list of tiles to filter each submesh of <see cref="surfaceMesh"/> to.
        /// Ignored unless <see cref="filterSurfaceSubmeshTiles"/> is true.
        /// </summary>
        public List<TileList> surfaceSubmeshTiles = new List<TileList>();


        /// <summary>
        /// Clear's previously generated content.
        /// </summary>
        public void Clear()
        {
            var output = GetComponent<ITesseraTileOutput>() ?? new InstantiateOutput(transform);
            output.ClearTiles();
        }

        /// <summary>
        /// Synchronously runs the generation process described in the class docs.
        /// </summary>
        /// <param name="onCreate">Called for each newly generated tile. By default, they are Instantiated in the scene.</param>
        public TesseraCompletion Generate(TesseraGenerateOptions options = null)
        {
            var e = StartGenerate(options);
            while (e.MoveNext())
            {
                var a = e.Current;
                if (a is TesseraCompletion tc)
                    return tc;
            }

            throw new Exception("Unreachable code.");
        }

        /// <summary>
        /// Runs Clear, then Generate
        /// </summary>
        public TesseraCompletion Regenerate(TesseraGenerateOptions options = null)
        {
            Clear();
            return Generate(options);
        }

        private Direction GetDirection(int i)
        {
            switch (i)
            {
                case 0: return Direction.XPlus;
                case 1: return Direction.ZPlus;
                case 2: return Direction.XMinus;
                case 3: return Direction.ZMinus;
            }
            throw new Exception();
        }

        private MeshTopologyBuilder BuildMesh(Mesh mesh)
        {
            var builder = new MeshTopologyBuilder();
            var edgeData = new Dictionary<(Vector3, Vector3), ((int, int), Direction)>();
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) != MeshTopology.Quads)
                {
                    throw new Exception($"Mesh topology {mesh.GetTopology(subMesh)} not supported. You need to select \"Keep Quads\"");
                }

                var indices = mesh.GetIndices(subMesh);
                for (var i = 0; i < indices.Length; i += 4)
                {
                    for (var e = 0; e < 4; e++)
                    {
                        var v1 = mesh.vertices[indices[i + e]];
                        var v2 = mesh.vertices[indices[i + (e + 1) % 4]];
                        if (edgeData.TryGetValue((v2, v1), out var data))
                        {
                            var face1 = i / 4;
                            var (face2, inverseDirection) = data;
                            var direction = GetDirection(e);
                            builder.Add((face1, subMesh), face2, direction, inverseDirection);
                        }
                        else
                        {
                            edgeData[(v1, v2)] = ((i / 4, subMesh), GetDirection(e));
                        }
                    }
                }
            }
            return builder;
        }

        internal TesseraGeneratorHelper CreateTesseraGeneratorHelper(TesseraGenerateOptions options = null)
        {
            options = options ?? new TesseraGenerateOptions();
            var progress = options.progress;

            var seed = options.seed == 0 ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : options.seed;

            var xororng = new XoRoRNG(seed);

            Validate();

            var actualInitialConstraints = new List<ITesseraInitialConstraint>();

            var tileModelInfo = TesseraGeneratorHelper.GetTileModelInfo(tiles);

            IExtendedTopology extendedTopology;

            TileModel tileModel;
            var cellType = new CubeCellType();
            if (surfaceMesh != null)
            {
                var builder = BuildMesh(surfaceMesh);
                var info = builder.GetInfo();
                var model = new GraphAdjacentModel(info);
                tileModel = model;

                // Build the extended topology
                extendedTopology = new MeshExtendedTopology(transform, cellType, surfaceMesh, size.y, tileSize.y, surfaceOffset, surfaceSmoothNormals, info, builder.GetTopology(size.y));

                foreach (var (tile, frequency) in tileModelInfo.AllTiles)
                {
                    model.SetFrequency(tile, frequency);
                }

                // Setup the model

                // TODO: Support internal adjacencies across other edge labels
                foreach (var (tile1, tile2, d) in tileModelInfo.InternalAdjacencies)
                {
                    foreach(var el in Enumerable.Range(0, info.EdgeLabelCount))
                    {
                        if(info.EdgeLabelInfo[el].Item3.IsIdentity && info.EdgeLabelInfo[el].Item1 == d)
                        {
                            model.AddAdjacency(tile1, tile2, (EdgeLabel)el);
                        }
                    }
                }

                var adjacencies = Enumerable.Range(0, info.EdgeLabelCount)
                    .Zip(info.EdgeLabelInfo, Tuple.Create)
                    .SelectMany(t => GetAdjacencies(palette, t.Item1, tileModelInfo.TilesByDirection[t.Item2.Item1], tileModelInfo.TilesByDirection[t.Item2.Item2]));
                foreach (var (t1, t2, el) in adjacencies)
                {
                    model.AddAdjacency(t1, t2, (EdgeLabel)el);
                }

                // surfaceSubmeshTiles
                if (filterSurfaceSubmeshTiles)
                {
                    foreach (var (subMesh, tileList) in surfaceSubmeshTiles.Select((x, i) => (i, x)))
                    {
                        var volumeFilter = new TesseraVolumeFilter
                        {
                            name = "Submesh " + subMesh.ToString(),
                            tiles = tileList.tiles,
                            mask = new bool[extendedTopology.Topology.IndexCount],
                        };
                        foreach (var index in extendedTopology.Topology.GetIndices())
                        {
                            var cell = extendedTopology.GetCell(index);
                            if (cell.z == subMesh)
                                volumeFilter.mask[index] = true;
                        }
                        actualInitialConstraints.Add(volumeFilter);
                    }
                }
            }
            else
            {

                var model = new AdjacentModel(DirectionSet.Cartesian3d);
                tileModel = model;
                extendedTopology = (IExtendedTopology)ExtendedTopology;

                foreach (var (tile, frequency) in tileModelInfo.AllTiles)
                {
                    model.SetFrequency(tile, frequency);
                }

                foreach (var (tile1, tile2, d) in tileModelInfo.InternalAdjacencies)
                {
                    model.AddAdjacency(tile1, tile2, d);
                }

                var adjacencies = cellType.GetDirectionPairs().SelectMany(t => GetAdjacencies(palette, t.Item1, tileModelInfo.TilesByDirection[t.Item1], tileModelInfo.TilesByDirection[t.Item2])).ToList();

                foreach (var (t1, t2, d) in adjacencies)
                {
                    model.AddAdjacency(t1, t2, d);
                }
            }

            var initialConstraints = options.initialConstraints ??
                (searchInitialConstraints ? (IEnumerable<ITesseraInitialConstraint>)GetInitialConstraints() : null ) ??
#pragma warning disable CS0618 // Type or member is obsolete
                this.initialConstraints;
#pragma warning restore CS0618 // Type or member is obsolete

            actualInitialConstraints.AddRange(initialConstraints);

            var constraints = GetTileConstraints(tileModel);

            var actualSkyBox = skyBox == null ? null : new TesseraInitialConstraint
            {
                faceDetails = skyBox.faceDetails,
                offsets = skyBox.offsets,
            };

            return new TesseraGeneratorHelper(
                            palette,
                            tileModel,
                            tileModelInfo,
                            actualInitialConstraints,
                            constraints,
                            extendedTopology,
                            backtrack,
                            actualSkyBox,
                            progress,
                            null,
                            xororng,
                            options.cancellationToken);
        }


        /// <summary>
        /// Asynchronously runs the generation process described in the class docs, for use with StartCoroutine.
        /// </summary>
        /// <remarks>The default instantiation is still synchronous, so this can still cause frame glitches unless you override onCreate.</remarks>
        /// <param name="onCreate"></param>
        public IEnumerator StartGenerate(TesseraGenerateOptions options = null)
        {
            options = options ?? new TesseraGenerateOptions();

            var coregenerator = CreateTesseraGeneratorHelper(options);


            for (var r = 0; r < retries; r++)
            {
                TilePropagator propagator;
                TilePropagator Run()
                {
                    coregenerator.SetupAndRun();
                    return coregenerator.Propagator;
                }

                if (options.multithreaded && Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    var runTask = Task.Run(Run,  options.cancellationToken);

                    while (!runTask.IsCompleted)
                        yield return null;

                    options.cancellationToken.ThrowIfCancellationRequested();

                    propagator = runTask.Result;
                }
                else
                {
                    propagator = Run();
                }

                var status = propagator.Status;

                var contradictionTile = new ModelTile {};

                var result = propagator.ToValueArray<ModelTile?>(contradiction: contradictionTile);


                if (status == DeBroglie.Resolution.Contradiction)
                {
                    if (r < retries - 1)
                    {
                        continue;
                    }
                }


                var completion = new TesseraCompletion();
                completion.retries = r;
                completion.backtrackCount = propagator.BacktrackCount;
                completion.success = status == DeBroglie.Resolution.Decided;
                completion.tileInstances = GetTesseraTileInstances(result, coregenerator.ExtendedTopology).ToList();
                completion.contradictionLocation = completion.success ? null : GetContradictionLocation(result);

                if (options.onComplete != null)
                {
                    options.onComplete(completion);
                }
                else
                {
                    HandleComplete(options, completion);
                }

                yield return completion;

                // Exit retries
                break;
            }
        }

        /// <summary>
        /// For validation purposes
        /// </summary>
        public IEnumerable<TesseraTile> GetMissizedTiles()
        {
            bool IsMissized(TesseraTile tile)
            {
                if (surfaceMesh != null) return false;
                if (size.x != 1 && tile.tileSize.x != tileSize.x) return true;
                if (size.y != 1 && tile.tileSize.y != tileSize.y) return true;
                if (size.z != 1 && tile.tileSize.z != tileSize.z) return true;

                return false;
            }

            return tiles.Select(x => x.tile)
                .Where(x => x != null)
                .Where(x => IsMissized(x));
        }

        /// <summary>
        /// Checks tiles are consistently setup
        /// </summary>
        internal void Validate()
        {
            var allTiles = tiles.Select(x => x.tile);
            if (surfaceMesh != null)
            {
                if (surfaceMesh.GetTopology(0) != MeshTopology.Quads)
                {
                    Debug.LogWarning($"Mesh topology {surfaceMesh.GetTopology(0)} not supported. You need to select \"Keep Quads\"");
                }
                if (!surfaceMesh.isReadable)
                {
                    Debug.LogWarning($"Surface mesh needs to be readable.");
                }
                //if (surfaceSmoothNormals && !surfaceMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
                if (surfaceSmoothNormals && surfaceMesh.tangents.Length == 0)
                {
                    Debug.LogWarning($"Surface mesh needs tangents to calculate smoothed normals. You need to select \"Calculate\" the tangent field of the import options.");
                }
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    Debug.LogWarning($"Some tiles have meshes that are not readable. They will not be transformed to fit the mesh. E.g {unreadable.First().name}");
                }
                if(filterSurfaceSubmeshTiles)
                {
                    for(var i=0;i< surfaceSubmeshTiles.Count;i++)
                    {
                        if(surfaceSubmeshTiles[i].tiles.Count == 0)
                        {
                            Debug.LogWarning($"Submesh {i} is filtered to zero tiles. Generation is impossible");
                        }
                    }
                }

                return;
            }
            var missizedTiles = GetMissizedTiles().ToList();
            if (missizedTiles.Count > 0)
            {
                Debug.LogWarning($"Some tiles do not have the same tileSize as the generator, {tileSize}, this can cause unexpected behaviour.\n" +
                    "NB: Big tiles should still share the same value of tileSize\n" +
                    "Affected tiles:\n" +
                    string.Join("\n", missizedTiles)
                    );
            }
        }

        /// <summary>
        /// Converts generator constraints into a format suitable for DeBroglie.
        /// </summary>
        private List<ITileConstraint> GetTileConstraints(TileModel model)
        {
            var l = new List<ITileConstraint>();
            foreach (var constraintComponent in GetComponents<TesseraConstraint>())
            {
                var constraint = constraintComponent.GetTileConstraint(model);
                l.Add(constraint);
            }
            return l;
        }

        /// <summary>
        /// Converts from DeBroglie's array format back to Tessera's.
        /// </summary>
        internal IEnumerable<TesseraTileInstance> GetTesseraTileInstances(ITopoArray<ModelTile?> result, IExtendedTopology et)
        {
            var topology = result.Topology;
            var mask = topology.Mask ?? Enumerable.Range(0, topology.IndexCount).Select(x => true).ToArray();

            var empty = mask.ToArray();
            for (var x = 0; x < topology.Width; x++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var z = 0; z < topology.Depth; z++)
                    {
                        var p = new Vector3Int(x, y, z);
                        // Skip if already filled
                        if (!empty[et.GetIndex(p)])
                            continue;
                        var modelTile = result.Get(x, y, z);
                        if (modelTile == null)
                            continue;
                        var rot = modelTile.Value.Rotation;
                        var tile = modelTile.Value.Tile;
                        if (tile == null)
                            continue;

                        var ti = GetTesseraTileInstance(x, y, z, modelTile.Value, et);

                        // Fill locations
                        foreach (var p2 in ti.Cells)
                        {
                            if (et.InBounds(p2))
                                empty[et.GetIndex(p2)] = false;
                        }

                        if (ti != null)
                        {
                            yield return ti;
                        }
                    }
                }
            }
        }

        private bool TryWalkOffset(Vector3Int initialCell, Vector3Int initialOffset, Vector3Int finalOffset, Rotation rotation, IExtendedTopology et, out Vector3Int finalCell)
        {
            var cellType = et.CellType;
            if (surfaceMesh == null)
            {
                if (!(cellType is CubeCellType))
                {
                    throw new NotImplementedException();
                }
                finalCell = initialCell + CubeGeometryUtils.Rotate(rotation, finalOffset - initialOffset);
                return true;
            }
            // Shortcut
            if (initialOffset == finalOffset)
            {
                finalCell = initialCell;
                return true;
            }

            var cell = initialCell;
            foreach(var stepDir in cellType.FindPath(initialOffset, finalOffset))
            {
                var dir = cellType.Rotate(stepDir, rotation);
                if(!et.TryMove(cell, dir, out cell, out var edgeRotation))
                {
                    finalCell = new Vector3Int();
                    return false;
                }
                if (!edgeRotation.IsIdentity)
                {
                    Debug.Log($"{initialCell} {initialOffset} {finalOffset} {rotation} {edgeRotation}");
                }
                rotation = edgeRotation * rotation;
            }
            finalCell = cell;
            return true;
        }

        internal TesseraTileInstance GetTesseraTileInstance(int x, int y, int z, ModelTile modelTile, IExtendedTopology et)
        {
            var rot = modelTile.Rotation;
            var tile = modelTile.Tile;
            var cellType = et.CellType;
            
            var p = new Vector3Int(x, y, z);

            var tileRotation = GeometryUtils.ToQuaternion(rot);
            var tileScale = rot.ReflectX ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);

            var localTrs = et.GetTRS(p) * new TRS(Vector3.zero, tileRotation, tileScale) * new TRS(-cellType.GetCellCenter(modelTile.Offset, tile.center, tileSize));
            var worldTrs = new TRS(transform) * localTrs;

            var newParent = gameObject.transform;

            Vector3Int[] cells;
                cells = tile.offsets.Select(offset =>
                {
                    if(!TryWalkOffset(p, modelTile.Offset, offset, rot, et, out var cell))
                    {
                        throw new Exception($"BigTile {modelTile.Tile} is not fully contained in topology. This indicates an internal error.");
                    }
                    return cell;
                }).ToArray();
            var instance =  new TesseraTileInstance
            {
                Tile = tile,
                Position = worldTrs.Position,
                Rotation = worldTrs.Rotation,
                LossyScale = worldTrs.Scale,
                LocalPosition = localTrs.Position,
                LocalRotation = localTrs.Rotation,
                LocalScale = localTrs.Scale,
                TileRotation = tileRotation,
                TileScale = tileScale,
                Cell = new Vector3Int(x, y, z),
                Cells = cells,
            };
            instance.MeshDeformation = surfaceMesh == null ? null : MeshUtils.GetDeformation(this, instance);
            return instance;
        }


        private Vector3Int? GetContradictionLocation(ITopoArray<ModelTile?> result)
        {
            var topology = result.Topology;
            var mask = topology.Mask ?? Enumerable.Range(0, topology.IndexCount).Select(x => true).ToArray();
            var et = ExtendedTopology;

            var empty = mask.ToArray();
            for (var x = 0; x < topology.Width; x++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var z = 0; z < topology.Depth; z++)
                    {
                        var p = new Vector3Int(x, y, z);
                        // Skip if already filled
                        if (!empty[et.GetIndex(p)])
                            continue;
                        var modelTile = result.Get(x, y, z);
                        if (modelTile == null)
                            continue;
                        var tile = modelTile.Value.Tile;
                        if (tile == null)
                        {
                            return new Vector3Int(x, y, z);
                        }
                    }
                }
            }

            return null;
        }

        private void HandleComplete(TesseraGenerateOptions options, TesseraCompletion completion)
        {
            if(!completion.success)
            {
                if (completion.contradictionLocation != null)
                {
                    var loc = completion.contradictionLocation;
                    Debug.LogError($"Failed to complete generation, issue at tile {loc}");
                }
                else
                {
                    Debug.LogError("Failed to complete generation");
                }
                return;
            }

            ITesseraTileOutput to = null;
            if(options.onCreate != null)
            {
                to = new ForEachOutput(options.onCreate);
            }
            else
            {
                to = GetComponent<ITesseraTileOutput>() ?? new InstantiateOutput(transform);
            }

            to.UpdateTiles(completion.tileInstances);
        }

        private static IEnumerable<(Tile, Tile, T)> GetAdjacencies<T>(TesseraPalette palette, T d, List<(FaceDetails, Tile)> tiles1, List<(FaceDetails, Tile)> tiles2)
        {
            foreach (var (fd1, t1) in tiles1)
            {
                foreach (var (fd2, t2) in tiles2)
                {
                    if (palette.Match(fd1, fd2))
                    {
                        yield return (t1, t2, d);
                    }
                }
            }
        }

        internal IExtendedTopologyLite ExtendedTopology
        {
            get
            {
                if (surfaceMesh != null)
                {
                    return new MeshExtendedTopology(transform, new CubeCellType(), surfaceMesh, size.y, tileSize.y, surfaceOffset, surfaceSmoothNormals, null, null);
                    
                }
                else
                {
                    return new GridExtendedTopology(transform, new CubeCellType(), center, size, tileSize);
                }
            }
        }

        internal ICellType CellType => new CubeCellType();

#region InitialConstraintUtilities

        /// <summary>
        /// Utility function that represents what <see cref="searchInitialConstraints"/> does.
        /// </summary>
        /// <returns>Initial constraints for use with <see cref="initialConstraints"/></returns>
        public List<ITesseraInitialConstraint> GetInitialConstraints()
        {
            IEnumerable<ITesseraInitialConstraint> tileConstraints = FindObjectsOfType(typeof(TesseraTile))
                .Cast<TesseraTile>()
                .Where(x => x != null)
                .Select(GetInitialConstraint);
            IEnumerable<ITesseraInitialConstraint> volumeConstraints = FindObjectsOfType(typeof(TesseraVolume))
                .Cast<TesseraVolume>()
                .Where(x => x != null)
                .Select(GetInitialConstraint);
            return tileConstraints.Concat(volumeConstraints)
                .Where(x => x != null)
                .ToList();
        }

        /// <summary>
        /// Utility function that gets the initial constraint from a given tile.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        /// <returns>Initial constraint for use with <see cref="initialConstraints"/></returns>
        public TesseraVolumeFilter GetInitialConstraint(TesseraVolume volume)
        {
            var et = ExtendedTopology;
            var colliders = volume.gameObject.GetComponents<Collider>();
            var mask = new bool[et.IndexCount];
            for (var i = 0; i < et.IndexCount; i++)
            {
                var center = et.GetCellCenter(et.GetCell(i));
                var worldCenter = transform.TransformPoint(center);
                var collides = false;
                foreach (var collider in colliders)
                {
                    if(collider.enabled && collider.ClosestPoint(worldCenter) == worldCenter)
                    {
                        collides = true;
                        break;
                    }
                }
                mask[i] = volume.invertArea ^ collides;
            }
            return new TesseraVolumeFilter
            {
                name = volume.name,
                mask = mask,
                tiles = volume.tiles,
            };
        }

        /// <summary>
        /// Utility function that gets the initial constraint from a given tile.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        /// <returns>Initial constraint for use with <see cref="initialConstraints"/></returns>
        public TesseraInitialConstraint GetInitialConstraint(TesseraTile tile)
        {
            return GetInitialConstraint(tile, tile.transform.localToWorldMatrix);
        }

        /// <summary>
        /// Utility function that gets the initial constraint from a given tile at a given position.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        /// <param name="localToWorldMatrix">The matrix indicating the position and rotation of the tile</param>
        /// <returns>Initial constraint for use with <see cref="initialConstraints"/></returns>
        public TesseraInitialConstraint GetInitialConstraint(TesseraTile tile, Matrix4x4 localToWorldMatrix)
        {
            if (!ExtendedTopology.GetCell(tile, localToWorldMatrix, out var cell, out var rotator))
            {
                return null;
            }
            // TODO: Needs support for big tiles
            return new TesseraInitialConstraint
            {
                name = tile.name,
                faceDetails = tile.faceDetails, // TODO: Need to copy for multithreading?
                offsets = tile.offsets, // TODO: Need to copy for multithreading?
                cell = cell,
                rotator = rotator,
            };
        }
#endregion

        /// <summary>
        /// Utility function that instantiates a tile instance in the scene.
        /// This is the default function used when you do not pass <c>onCreate</c> to the Generate method.
        /// It is essentially the same as Unity's normal Instantiate method with extra features:
        /// * respects <see cref="TesseraTileBase.instantiateChildrenOnly"/>
        /// * applies mesh transformations (Pro only)
        /// </summary>
        /// <param name="instance">The instance being created.</param>
        /// <param name="parent">The game object to parent the new game object to. This does not affect the world position of the instance</param>
        /// <returns>The game objects created.</returns>
        public static GameObject[] Instantiate(TesseraTileInstance instance, Transform parent)
        {
            var gameObjects = InstantiateUntransformed(instance, parent);
            if (instance.MeshDeformation != null)
            {
                var cell = instance.Cells.First();
                // MeshDeformation maps vertices into to generator space, but the instances already have a transform which needs undoing
                var meshDeformation = new TRS(instance.LocalPosition, instance.LocalRotation, instance.LocalScale).ToMatrix().inverse * instance.MeshDeformation;
                foreach (var go in gameObjects)
                {
                    MeshUtils.TransformRecursively(go, meshDeformation);
                }
            }
            return gameObjects;
        }


        private static GameObject[] InstantiateUntransformed(TesseraTileInstance instance, Transform parent)
        {
            if (instance.Tile.instantiateChildrenOnly)
            {
                var worldTransform = Matrix4x4.TRS(instance.Position, instance.Rotation, instance.LossyScale);
                var localTransform = Matrix4x4.TRS(instance.LocalPosition, instance.LocalRotation, instance.LossyScale);
                return instance.Tile.gameObject.transform.Cast<Transform>().Select(child =>
                {
                    var local = new TRS(localTransform * instance.Tile.transform.worldToLocalMatrix * child.transform.localToWorldMatrix);
                    var world = new TRS(worldTransform * instance.Tile.transform.worldToLocalMatrix * child.transform.localToWorldMatrix);
                    var go = GameObject.Instantiate(child.gameObject,world.Position, world.Rotation, parent);
                    go.transform.localScale = local.Scale;
                    return go;
                }).ToArray();
            }
            else
            {
                var go = GameObject.Instantiate(instance.Tile.gameObject, instance.Position, instance.Rotation, parent);
                go.transform.localScale = instance.LocalScale;
                return new[] { go };
            }
        }
    }
}