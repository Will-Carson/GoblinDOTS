using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tessera;

public class GeneratorController : MonoBehaviour
{
    public TesseraTile northGate;
    public TesseraTile southGate;
    public TesseraTile eastGate;
    public TesseraTile westGate;
    public TesseraTile hatchUp;
    public TesseraTile hatchDown;

    public List<TesseraGenerator> generators = new List<TesseraGenerator>();
    public List<bool> generatorDone = new List<bool>();

    public void GenerateWorld(int generatorId)
    {
        var t = new Task(GenerateWorldEnum(generatorId));
    }

    public IEnumerator GenerateWorldEnum(int generatorId)
    {
        while (!generatorDone[generatorId])
        {
            var timer = Time.time + 5;
            generators[generatorId].Clear();
            Task t = new Task(generators[generatorId].StartGenerate());
            while (t.Running && timer > Time.time)
            {
                yield return null;
            }
            if (!t.Running)
            {
                generatorDone[generatorId] = true;
            }
        }
    }
}
