using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    // Cubes which rotate in the X-Z plane
    public class CubeCellType : ICellType
    {
        public static CubeCellType Instance => new CubeCellType();

        public IEnumerable<FaceDir> GetFaceDirs() => new[]
        {
            FaceDir.Right,
            FaceDir.Left,
            FaceDir.Up,
            FaceDir.Down,
            FaceDir.Forward,
            FaceDir.Back,
        };

        public IEnumerable<Direction> GetDirections() => new[]
        {
            Direction.XPlus,
            Direction.XMinus,
            Direction.YPlus,
            Direction.YMinus,
            Direction.ZPlus,
            Direction.ZMinus,
        };

        public IEnumerable<(Direction, Direction)> GetDirectionPairs() => new[]
        {
            (Direction.XPlus, Direction.XMinus),
            (Direction.YPlus, Direction.YMinus),
            (Direction.ZPlus, Direction.ZMinus),
        };

        public Direction Invert(Direction d)
        {
            return DirectionSet.Cartesian3d.Inverse(d);
        }

        public RotationGroup GetRotationGroup() => new RotationGroup(4, true);

        public Direction Rotate(FaceDir faceDir, Rotation rotation)
        {
            if (faceDir == FaceDir.Up)
                return Direction.YPlus;
            if (faceDir == FaceDir.Down)
                return Direction.YMinus;

            var f = faceDir.Forward();
            var x1 = (int)f.x;
            var z1 = (int)f.z;
            var (x2, z2) = TopoArrayUtils.SquareRotateVector(x1, z1, rotation);
            if (x2 == 1)
                return Direction.XPlus;
            if (x2 == -1)
                return Direction.XMinus;
            if (z2 == 1)
                return Direction.ZPlus;
            if (z2 == -1)
                return Direction.ZMinus;
            throw new System.Exception();
        }

        public (Direction, FaceDetails) RotateBy(FaceDir faceDir, FaceDetails faceDetails, Rotation rot)
        {
            var rotatedDirection = Rotate(faceDir, rot);
            return (
                rotatedDirection,
                CubeGeometryUtils.RotateBy(faceDetails, rotatedDirection, rot)
                );

        }

        public bool TryMove(Vector3Int offset, FaceDir dir, out Vector3Int dest)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FaceDir> FindPath(Vector3Int startOffset, Vector3Int endOffset)
        {
            var offset = startOffset;
            while (offset.x < endOffset.x)
            {
                yield return FaceDir.Right;
                offset.x += 1;
            }
            while (offset.x > endOffset.x)
            {
                yield return FaceDir.Left;
                offset.x -= 1;
            }
            while (offset.y < endOffset.y)
            {
                yield return FaceDir.Up;
                offset.y += 1;
            }
            while (offset.y > endOffset.y)
            {
                yield return FaceDir.Down;
                offset.y -= 1;
            }
            while (offset.z < endOffset.z)
            {
                yield return FaceDir.Forward;
                offset.z += 1;
            }
            while (offset.z > endOffset.z)
            {
                yield return FaceDir.Back;
                offset.z -= 1;
            }
        }

        public (Direction, FaceDetails) ApplyRotator(FaceDir faceDir, FaceDetails faceDetails, object rotator)
        {
            return CubeGeometryUtils.ApplyRotator(faceDir, faceDetails, (MatrixInt3x3)rotator);
        }

        public Vector3 GetCellCenter(Vector3Int offset, Vector3 center, Vector3 tileSize)
        {
            return center + Vector3.Scale(offset, tileSize);
        }
    }
}
