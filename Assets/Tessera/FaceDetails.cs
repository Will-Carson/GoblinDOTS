using DeBroglie.Rot;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Records the painted colors for a single face of one cube in a <see cref="TesseraTile"/>
    /// </summary>
    [Serializable]
    public class FaceDetails
    {
        public int topLeft;
        public int top;
        public int topRight;
        public int left;
        public int center;
        public int right;
        public int bottomLeft;
        public int bottom;
        public int bottomRight;


        // TODO: These mutating methods should not be needed publically
        internal FaceDetails Clone()
        {
            return (FaceDetails)MemberwiseClone();
        }

        internal void ReflectX()
        {
            (topLeft, topRight) = (topRight, topLeft);
            (left, right) = (right, left);
            (bottomLeft, bottomRight) = (bottomRight, bottomLeft);
        }

        internal void RotateCw()
        {
            (topLeft, topRight, bottomRight, bottomLeft) = (topRight, bottomRight, bottomLeft, topLeft);
            (top, right, bottom, left) = (right, bottom, left, top);
        }

        public override string ToString()
        {
            return $"({topLeft},{top},{topRight};{left},{center},{right};{bottomLeft},{bottom},{bottomRight})";
        }
    }
}