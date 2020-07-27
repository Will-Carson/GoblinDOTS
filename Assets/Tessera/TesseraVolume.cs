using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    [AddComponentMenu("Tessera/Tessera Volume", 21)]
    public class TesseraVolume : MonoBehaviour
    {
        /// <summary>
        /// No effect on behaviour, setting this improves the UI in the Unity inspector.
        /// </summary>
        public TesseraGenerator generator;

        /// <summary>
        /// The list of tiles to filter on.
        /// </summary>
        public List<TesseraTileBase> tiles;

        /// <summary>
        /// If false, affect all cells inside the volume's colliders.
        /// If true, affect all cells outside.
        /// </summary>
        public bool invertArea;
    }
}
