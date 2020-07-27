using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using UnityEngine;

namespace Tessera
{
    public static class FaceDirExtensions
    {
        /// <returns>Returns (0, 1, 0) vector for most faces, and returns (0, 0, 1) for the top/bottom faces.</returns>
        public static Vector3Int Up(this FaceDir faceDir)
        {
            switch (faceDir)
            {
                case FaceDir.Left:
                case FaceDir.Right:
                case FaceDir.Forward:
                case FaceDir.Back:
                    return Vector3Int.up;
                case FaceDir.Up:
                case FaceDir.Down:
                    return new Vector3Int(0, 0, 1);
            }
            throw new Exception();
        }

        /// <returns>The normal vector for a given face.</returns>
        public static Vector3Int Forward(this FaceDir faceDir)
        {
            switch (faceDir)
            {
                case FaceDir.Left: return Vector3Int.left;
                case FaceDir.Right: return Vector3Int.right;
                case FaceDir.Up: return Vector3Int.up;
                case FaceDir.Down: return Vector3Int.down;
                case FaceDir.Forward: return new Vector3Int(0, 0, 1);
                case FaceDir.Back: return new Vector3Int(0, 0, -1);
            }
            throw new Exception();
        }

        /// <returns>Returns the face dir with the opposite normal vector.</returns>
        public static FaceDir Inverted(this FaceDir faceDir)
        {

            switch (faceDir)
            {
                case FaceDir.Left: return FaceDir.Right;
                case FaceDir.Right: return FaceDir.Left;
                case FaceDir.Up: return FaceDir.Down;
                case FaceDir.Down: return FaceDir.Up;
                case FaceDir.Forward: return FaceDir.Back;
                case FaceDir.Back: return FaceDir.Forward;
            }
            throw new Exception();
        }

        /// <summary>
        /// Convert from Tessera enum to DeBroglie enum.
        /// </summary>
        internal static Direction ToDirection(this FaceDir faceDir)
        {
            switch (faceDir)
            {
                case FaceDir.Left: return Direction.XMinus;
                case FaceDir.Right: return Direction.XPlus;
                case FaceDir.Up: return Direction.YPlus;
                case FaceDir.Down: return Direction.YMinus;
                case FaceDir.Forward: return Direction.ZPlus;
                case FaceDir.Back: return Direction.ZMinus;
            }
            throw new Exception();
        }
    }
}