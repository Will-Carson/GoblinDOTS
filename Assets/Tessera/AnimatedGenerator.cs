using DeBroglie.Topo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Attach this to a TesseraGenerator to run the generator stepwise over several updates,
    /// displaying the changes so far.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/Animated Generator", 41)]
    public class AnimatedGenerator : MonoBehaviour
    {
        private bool started = false;
        private bool running = false;

        private TesseraGeneratorHelper helper;

        private DeBroglie.Topo.ITopoArray<ISet<ModelTile>> lastTiles;

        private float lastStepTime = 0.0f;
        private DateTime baseTime = DateTime.Now;

        private ITesseraTileOutput tileOutput;

        private bool supportsCubes;

        private bool[] hasObject;
        private GameObject[] cubesByIndex;
        private bool firstStep;

        public float secondsPerStep = .0f;

        public float progressPerStep = 1;

        public bool IsRunning => running;

        public bool IsStarted => started;

        /// <summary>
        /// Game object to show in cells that have yet to be fully solved.
        /// </summary>
        public GameObject uncertaintyTile;

        /// <summary>
        /// If true, the uncertainty tiles shrink as the solver gets more certain.
        /// </summary>
        public bool scaleUncertainyTile = true;

        public void StartGeneration()
        {
            if (running)
                return;

            if (started)
                StopGeneration();

            tileOutput?.ClearTiles();

            var generator = GetComponent<TesseraGenerator>();
            helper = generator.CreateTesseraGeneratorHelper();
            helper.Setup();
            // TODO: Should be *all tiles* to start with
            lastTiles = helper.Propagator.ToValueSets<ModelTile>();
            baseTime = DateTime.Now;
            lastStepTime = GetTime();
            tileOutput = generator.GetComponent<ITesseraTileOutput>() ?? new UpdatableInstantiateOutput(generator, transform);
            if(!tileOutput.SupportsIncremental)
            {
                throw new Exception($"Output {tileOutput} does not support animations");
            }
            supportsCubes = generator.GetComponent<ITesseraTileOutput>() == null;
            hasObject = new bool[helper.Propagator.Topology.IndexCount];
            cubesByIndex = new GameObject[helper.Propagator.Topology.IndexCount];
            firstStep = true;
            started = true;

            ResumeGeneration();
        }

        public void ResumeGeneration()
        {
            if (running)
                return;

            if (!started)
            {
                throw new Exception("Generation must be started first.");
            }

            running = true;
        }

        public void PauseGeneration()
        {
            if (!running)
                return;

            running = false;
        }

        public void StopGeneration()
        {
            if (!started)
                return;

            PauseGeneration();
            tileOutput?.ClearTiles();
            tileOutput = null;
            if (helper != null)
            {
                foreach (var i in helper.Propagator.Topology.GetIndices())
                {
                    ClearCube(i);
                }
            }
            started = false;
        }

        private void Update()
        {
            Step();
        }

        void ClearCube(int i)
        {
            if (cubesByIndex[i] != null)
            {
                ClearCube(cubesByIndex[i]);
            }
            cubesByIndex[i] = null;
        }

        void ClearCube(GameObject cube)
        {
            DestroyImmediate(cube);
        }

        GameObject CreateCube(TesseraGenerator generator, IExtendedTopology et, Vector3Int p)
        {
            //var c = Instantiate(uncertaintyTile, generator.transform.TransformPoint(et.GetCellCenter(p)), Quaternion.identity, generator.transform);
            var trs = new TRS(generator.transform) * et.GetTRS(p);
            var c = Instantiate(uncertaintyTile, trs.Position, trs.Rotation, generator.transform);
            c.transform.localScale = trs.Scale;

            return c;

        }

        public void Step()
        {
            if (!running) return;

            if (gameObject == null)
            {
                StopGeneration();
            }

            if (GetTime() < lastStepTime + secondsPerStep) return;

            var generator = GetComponent<TesseraGenerator>();

            var propagator = helper.Propagator;
            var topology = propagator.Topology;
            var et = helper.ExtendedTopology;

            for (var i = 0; i < progressPerStep; i++)
            {
                if (propagator.Status != DeBroglie.Resolution.Undecided)
                    break;

                propagator.Step();
            }
            lastStepTime = GetTime();
            if(propagator.Status != DeBroglie.Resolution.Undecided)
            {
                started = false;
                PauseGeneration();
            }

            var tiles = propagator.ToValueSets<ModelTile>();

            var mask = topology.Mask ?? Enumerable.Range(0, topology.IndexCount).Select(x => true).ToArray(); ;
            var maskOrProcessed = mask.ToArray();

            var updateInstances = new List<TesseraTileInstance>();

            var tileCount = helper.Propagator.TileModel.Tiles.Count();
            var minScale = 0.0f;
            var maxScale = 1.0f;

            foreach (var i in topology.GetIndices())
            {
                // Skip indices that are masked out or already processed
                if (!maskOrProcessed[i])
                    continue;

                var before = lastTiles.Get(i);
                var after = tiles.Get(i);

                // Skip if nothing has changed
                var hasChanged = !before.SetEquals(after);
                if (!firstStep && !hasChanged)
                    continue;


                if (after.Count == 1)
                {
                    topology.GetCoord(i, out var x, out var y, out var z);
                    var p = new Vector3Int(x, y, z);

                    var modelTile = after.Single();
                    var ti = generator.GetTesseraTileInstance(x, y, z, modelTile, et);
                    foreach (var p2 in ti.Cells)
                    {
                        if (et.InBounds(p2))
                        {
                            var i2 = et.GetIndex(p2);
                            ClearCube(i2);
                            maskOrProcessed[i2] = true;
                            hasObject[i2] = true;
                        }
                    }

                    updateInstances.Add(ti);
                }
                else if(hasChanged)
                {
                    var p = et.GetCell(i);

                    maskOrProcessed[i] = true;

                    // Draw cube
                    ClearCube(i);
                    if (uncertaintyTile != null && supportsCubes)
                    {
                        var c = cubesByIndex[i] = CreateCube(generator, et, p);
                        var scale = (maxScale - minScale) * after.Count / tileCount + minScale;
                        if (scaleUncertainyTile)
                        {
                            c.transform.localScale = c.transform.localScale * scale;
                        }
                    }

                    // Remove object
                    if (hasObject[i])
                    {
                        updateInstances.Add(new TesseraTileInstance
                        {
                            Cells = new[] { p }
                        });
                        hasObject[i] = false;
                    }
                    
                }
            }

            tileOutput.UpdateTiles(updateInstances);
            lastTiles = tiles;
            firstStep = false;
        }

        private float GetTime()
        {
            if (Application.isPlaying)
            {
                return Time.time;
            }
            else
            {
                return (float)(DateTime.Now - baseTime).TotalSeconds;
            }
        }
    }
}
