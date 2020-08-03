using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class ServerClientDataSplitter : MonoBehaviour
{
    public List<FullPlayData> fullPlays = new List<FullPlayData>();
    private World client;
    private World server;

    private int dialogueCounter = 0;
    private int requirementCounter = 0;
    private int lineCounter = 0;

    private bool isClient;
    private bool isServer;

    void Start()
    {
        // TODO TEST
        #region Test data

        var testPlay = new FullPlayData();

        var parameters = new List<Parameter>();
        var p = new Parameter
        {
            op = Operator.LessOrEqual,
            type = ParameterType.NumberOfActors,
            value1 = 2
        };
        parameters.Add(p);

        var contents = new List<DialogueContent>();
        var c = new DialogueContent
        {
            line = "Dong"
        };
        contents.Add(c);

        var lines = new List<Line>();
        var l = new Line
        {
            dialogueId = 0,
            isEnd = true,
            life = 10,
            speaker = 0,
            childA = 0
        };
        lines.Add(l);

        testPlay.requirements = parameters;
        testPlay.content = contents;
        testPlay.lines = lines;
        fullPlays.Add(testPlay);

        #endregion

        isClient = false;
        isServer = false;
        var worlds = World.All;
        foreach (var w in worlds)
        {
            if (w.Name == "ClientWorld") { isClient = true; client = w; }
            if (w.Name == "ServerWorld") { isServer = true; server = w; }
        }

        TryBuildPlayData();
    }

    private void TryBuildPlayData()
    {
        DialogueManager dialogueManager = null;
        SystemParameterAnalyzer parameterAnalyzer = null;
        SystemRunPlay playRunner = null;

        if (isClient) { dialogueManager = FindObjectOfType<DialogueManager>(); }
        if (isServer) { parameterAnalyzer = server.GetOrCreateSystem<SystemParameterAnalyzer>(); }
        if (isServer) { playRunner = server.GetOrCreateSystem<SystemRunPlay>(); }

        for (int i = 0; i < fullPlays.Count; i++)
        {
            var p = fullPlays[i];
            var dialogueIds = new List<int>();
            if (isClient)
            {
                for (int j = 0; j < p.content.Count; j++)
                {
                    // Add content to DialogueManager
                    dialogueManager.content.Add(dialogueCounter, p.content[j]);
                    dialogueIds.Add(dialogueCounter);
                    dialogueCounter++;
                }
            }

            if (isServer)
            {
                var tempLineCounter = lineCounter;
                var lineIds = new List<int>();
                for (int j = 0; j < p.lines.Count; j++)
                {
                    // Add lines to SystemRunPlay
                    lineIds.Add(tempLineCounter);
                    tempLineCounter++;
                }

                for (int j = 0; j < p.lines.Count; j++)
                {
                    // Add lines to SystemRunPlay
                    var line = p.lines[j];
                    var l = new Line
                    {
                        dialogueId = dialogueIds[line.dialogueId],
                        isEnd = line.isEnd,
                        life = line.life,
                        speaker = line.speaker,
                        childA = lineIds[line.childA],
                        childB = lineIds[line.childB],
                        childC = lineIds[line.childC],
                        childD = lineIds[line.childD]
                    };
                    playRunner.PlayLibrary.Add(lineCounter, l);
                    lineCounter++;
                }

                for (int j = 0; j < p.requirements.Count; j++)
                {
                    // Add requirements to SystemParameterAnalyzer
                    parameterAnalyzer.Plays.Add(i, p.requirements[j]);
                }

                parameterAnalyzer.PlayDramaValues.Add(i, fullPlays[i].drama);
            }
        }
    }
}

public class FullPlayData
{
    // Server data
    public List<Parameter> requirements = new List<Parameter>();
    public List<Line> lines = new List<Line>();
    public int drama = 0;

    // Client data
    public List<DialogueContent> content = new List<DialogueContent>();
}