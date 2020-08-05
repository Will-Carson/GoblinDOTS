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
            op = Operator.Equal,
            type = ParameterType.NumberOfActors,
            value1 = 1
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

        testPlay.drama = 2;
        testPlay.requirements = parameters;
        testPlay.content = contents;
        testPlay.lines = lines;
        fullPlays.Add(testPlay);

        var testPlay1 = new FullPlayData();

        var parameters1 = new List<Parameter>();
        var p1 = new Parameter
        {
            op = Operator.Equal,
            type = ParameterType.NumberOfActors,
            value1 = 3
        };
        parameters1.Add(p1);

        var contents1 = new List<DialogueContent>();
        var c1 = new DialogueContent
        {
            line = "Dong"
        };
        var c2 = new DialogueContent
        {
            line = "Dong1"
        };
        var c3 = new DialogueContent
        {
            line = "Dong2"
        };
        var c4 = new DialogueContent
        {
            line = "Dong3"
        };
        contents1.Add(c1);
        contents1.Add(c2);
        contents1.Add(c3);
        contents1.Add(c4);

        var lines1 = new List<Line>();
        var l1 = new Line
        {
            dialogueId = 0,
            isEnd = false,
            life = 4,
            speaker = 0,
            childA = 1
        };
        var l2 = new Line
        {
            dialogueId = 1,
            isEnd = false,
            life = 4,
            speaker = 0,
            childA = 2
        };
        var l3 = new Line
        {
            dialogueId = 2,
            isEnd = false,
            life = 4,
            speaker = 0,
            childA = 3
        };
        var l4 = new Line
        {
            dialogueId = 3,
            isEnd = false,
            life = 4,
            speaker = 0
        };
        lines1.Add(l1);
        lines1.Add(l2);
        lines1.Add(l3);
        lines1.Add(l4);
        lines1.Reverse();

        testPlay1.drama = 2;
        testPlay1.requirements = parameters1;
        testPlay1.content = contents1;
        testPlay1.lines = lines1;
        fullPlays.Add(testPlay1);

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
        ParameterAnalyzer parameterAnalyzer = null;
        StartPlay playStarter = null;
        RunPlay playRunner = null;

        if (isClient) { dialogueManager = FindObjectOfType<DialogueManager>(); }
        if (isServer) { parameterAnalyzer = server.GetOrCreateSystem<ParameterAnalyzer>(); }
        if (isServer) { playStarter = server.GetOrCreateSystem<StartPlay>(); }
        if (isServer) { playRunner = server.GetOrCreateSystem<RunPlay>(); }

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
                var lineIds = new List<int>();
                for (int j = 0; j < p.lines.Count; j++)
                {
                    // Add lines to SystemRunPlay
                    lineIds.Add(lineCounter);
                    lineCounter++;
                }

                for (int j = 0; j < p.lines.Count; j++)
                {
                    // Add lines to SystemRunPlay
                    var line = p.lines[j];
                    Debug.Log(p.lines[j].dialogueId);
                    //Debug.Log(dialogueIds[p.lines[j].dialogueId]);
                    var l = new Line
                    {
                        // TODO fix
                        dialogueId = dialogueIds[p.lines[j].dialogueId],
                        isEnd = line.isEnd,
                        life = line.life,
                        speaker = line.speaker,
                        childA = lineIds[line.childA],
                        childB = lineIds[line.childB],
                        childC = lineIds[line.childC],
                        childD = lineIds[line.childD]
                    };
                    playStarter.PlayLibrary.Add(i, l);
                }

                playRunner.PlayLibrary = playStarter.PlayLibrary;

                for (int j = 0; j < p.requirements.Count; j++)
                {
                    // Add requirements to SystemParameterAnalyzer
                    parameterAnalyzer.PlaysRequirements.Add(i, p.requirements[j]);
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