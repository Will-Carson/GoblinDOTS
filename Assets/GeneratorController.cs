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

    public List<TesseraGenerator> generators = new List<TesseraGenerator>();

    public bool isDone = false;

    public void GenerateWorld()
    {
        var t = new Task(GenerateWorldEnum());
    }

    public IEnumerator GenerateWorldEnum()
    {
        foreach (var g in generators)
        {
            var t = new Task(g.StartGenerate());
            while (t.Running) yield return null;
        }

        isDone = true;
    }
}
