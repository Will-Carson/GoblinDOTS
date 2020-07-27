using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Face = System.ValueTuple<int, int>;

namespace Tessera
{
    // Functions similarly to DeBroglie.MeshTopologyBuilder
    // except the mesh is on the X-Z plane, and Y-axis is used for repeat copies
    internal class MeshTopologyBuilder
    {
        private DirectionSet directions;

        private int edgeLabelCount;

        // By index, direction
        private IDictionary<(Face, Direction), NeighbourDetails> neighbours;

        private int faceCount = 0;
        private int subMeshCount = 0;

        private readonly Dictionary<(Face, Face), Direction> pendingInverses = new Dictionary<(Face, Face), Direction>();

        public MeshTopologyBuilder()
        {
            this.directions = DirectionSet.Cartesian3d;
            edgeLabelCount = 4 * 4 + 2;
            neighbours = new Dictionary<(Face, Direction), NeighbourDetails>();
        }

        private int GetAngle(Direction d)
        {
            switch (d)
            {
                case Direction.XPlus: return 0;
                case Direction.ZPlus: return 90;
                case Direction.XMinus: return 180;
                case Direction.ZMinus: return 270;
            }
            throw new Exception();
        }

        private int GetHalfEdgeLabel(Direction d)
        {

            switch (d)
            {
                case Direction.XPlus: return 0;
                case Direction.ZPlus: return 1;
                case Direction.XMinus: return 2;
                case Direction.ZMinus: return 3;
            }
            throw new Exception();
        }

        private Direction FromHalfEdgeLabel(int he)
        {

            switch (he)
            {
                case 0: return Direction.XPlus;
                case 1: return Direction.ZPlus;
                case 2: return Direction.XMinus;
                case 3: return Direction.ZMinus;
            }
            throw new Exception();
        }

        private Rotation GetRotation(Direction direction, Direction inverseDirection)
        {
            return new Rotation((360 + GetAngle(direction) - GetAngle(inverseDirection) + 180) % 360);
        }

        private EdgeLabel GetEdgeLabel(Direction direction, Direction inverseDirection)
        {
            if(direction == Direction.YPlus)
            {
                return (EdgeLabel)16;
            }
            if (direction == Direction.YMinus)
            {
                return (EdgeLabel)17;
            }
            return (EdgeLabel)(GetHalfEdgeLabel(direction) + 4 * GetHalfEdgeLabel(inverseDirection));
        }

        /// <summary>
        /// Registers face1 and face2 as adjacent, moving in direction from face1 to face2.
        /// If you call this, you will also need to call add with face1 and face2 swapped, to
        /// establish the direction when travelling back.
        /// </summary>
        public void Add(Face face1, Face face2, Direction direction)
        {
            if (pendingInverses.TryGetValue((face2, face1), out var inverseDirection))
            {
                Add(face1, face2, direction, inverseDirection);
                pendingInverses.Remove((face2, face1));
            }
            else
            {
                pendingInverses.Add((face1, face2), direction);
            }
        }

        /// <summary>
        /// Registers face1 and face2 as adjacent, moving in direction from face1 to face2 and inverseDirection from face2 to face1.
        /// </summary>
        public void Add(Face face1, Face face2, Direction direction, Direction inverseDirection)
        {
            faceCount = Math.Max(faceCount, Math.Max(face1.Item1, face2.Item1) + 1);
            subMeshCount = Math.Max(subMeshCount, Math.Max(face1.Item2, face2.Item2) + 1);
            neighbours[(face1, direction)] = new NeighbourDetails
            {
                Face = face2,
                InverseDirection = inverseDirection,
                EdgeLabel = GetEdgeLabel(direction, inverseDirection)
            };
            neighbours[(face2, inverseDirection)] = new NeighbourDetails
            {
                Face = face1,
                InverseDirection = direction,
                EdgeLabel = GetEdgeLabel(inverseDirection, direction)
            };
        }

        public SubMeshTopology GetTopology(int height)
        {
            int GetIndex(int face, int layer, int subMesh)
            {
                return face + layer * faceCount + subMesh * faceCount * height;
            }

            if (pendingInverses.Count > 0)
            {
                var kv = pendingInverses.First();
                throw new Exception($"Some face adjacencies have only been added in one direction, e.g. {kv.Key.Item1} -> {kv.Key.Item2}");
            }
            var allNeighbours = new SubMeshTopology.NeighbourDetails[height * faceCount * subMeshCount, directions.Count];
            var mask = new bool[height * faceCount * subMeshCount];
            for(var y=0;y<height;y++)
            {
                for(var x=0;x<faceCount;x++)
                {
                    for(var z = 0;z<subMeshCount;z++)
                    {
                        var index = GetIndex(x, y, z);
                        var exists = false;
                        // Add horizontal neighbours
                        for (var d = 0; d < directions.Count; d++)
                        {
                            if(neighbours.TryGetValue(((x, z), (Direction)d), out var neighbourDetails))
                            {
                                var neighbourIndex = GetIndex(neighbourDetails.Face.Item1, y, neighbourDetails.Face.Item2);
                                allNeighbours[index, d] = new SubMeshTopology.NeighbourDetails
                                {
                                    Index = neighbourIndex,
                                    EdgeLabel = neighbourDetails.EdgeLabel,
                                    InverseDirection = neighbourDetails.InverseDirection,
                                };
                                exists = true;
                            }
                            else
                            {
                                allNeighbours[index, d].Index = -1;
                            }
                        }
                        mask[index] = exists;
                        // Add vertical neighbours
                        allNeighbours[index, (int)Direction.YPlus] = new SubMeshTopology.NeighbourDetails
                        {
                            Index = exists && y == height - 1 ? -1 : GetIndex(x, y - 1, z),
                            EdgeLabel = (EdgeLabel)(4 * 4),
                            InverseDirection = Direction.YMinus,
                        };
                        allNeighbours[index, (int)Direction.YMinus] = new SubMeshTopology.NeighbourDetails
                        {
                            Index = exists && y == 0 ? -1 : GetIndex(x, y - 1, z),
                            EdgeLabel = (EdgeLabel)(4 * 4 + 1),
                            InverseDirection = Direction.YPlus,
                        };
                    }
                }
            }
            return new SubMeshTopology(allNeighbours, faceCount, height, subMeshCount, mask);
        }

        public GraphInfo GetInfo() => new GraphInfo
        {
            DirectionsCount = directions.Count,
            EdgeLabelCount = edgeLabelCount,
            EdgeLabelInfo = (from el in Enumerable.Range(0, 16)
                             let d = FromHalfEdgeLabel(el % 4)
                             let id = FromHalfEdgeLabel(el / 4)
                             select (d, id, GetRotation(d, id)))
            .Concat(new[]
            {
                (Direction.YPlus, Direction.YMinus, new Rotation()),
                (Direction.YMinus, Direction.YPlus, new Rotation()),
            })
            .ToArray(),
        };

        private struct NeighbourDetails
        {
            public Face Face { get; set; }

            /// <summary>
            /// The edge label of this edge
            /// </summary>
            public EdgeLabel EdgeLabel { get; set; }

            /// <summary>
            /// The direction to move from Index which will return back along this edge.
            /// </summary>
            public Direction InverseDirection { get; set; }
        }

    }
}
