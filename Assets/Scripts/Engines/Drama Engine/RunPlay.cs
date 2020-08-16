using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class RunPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    public NativeMultiHashMap<int, Line> PlayLibrary 
        = new NativeMultiHashMap<int, Line>(G.numberOfPlays * G.linesPerPlay, Allocator.Persistent);

    protected override void OnDestroy()
    {
        PlayLibrary.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().AsParallelWriter();
        var playLibrary = PlayLibrary;
        var time = Time.ElapsedTime;

        // Increment lines and generate step requests
        Entities
        .WithNone<NeedsPlay>()
        .WithAll<Line>()
        .ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in PlayRunner playRunner) =>
        {
            // generate step request if time is up for running line
            if (playRunner.lineTime > playRunner.lineTimeMax)
            {
                ecb.AppendToBuffer(entityInQueryIndex, entity, new PlayLineRequest { newLine = 0 });
                return;
            }

            // increment play line time
            // TODO Find a better way
            if (playRunner.lineTimeMax == 0) return;
            var dif = time - playRunner.timeLineUpdated;
            var newPlayRunner = playRunner;
            newPlayRunner.lineTime = dif;

            ecb.SetComponent(entityInQueryIndex, entity, newPlayRunner);
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);

        // Process step requests, release stages for new plays, create dialogue requests
        Entities
        .ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in PlayRunner playRunner,
            in Line playingLine,
            in DynamicBuffer<PlayLineRequest> newLines,
            in PlayActorIds actors) =>
        {
            for (int j = 0; j < newLines.Length; j++)
            {
                var newLine = newLines[j];

                // Process step requests
                var newPlayRunner = new PlayRunner();
                if (newLine.newLine == 0) { newPlayRunner.lineId = playingLine.childA; }
                if (newLine.newLine == 1) { newPlayRunner.lineId = playingLine.childB; }
                if (newLine.newLine == 2) { newPlayRunner.lineId = playingLine.childC; }
                if (newLine.newLine == 3) { newPlayRunner.lineId = playingLine.childD; }
                
                var nextLine = new Line();
                var playLines = playLibrary.GetValuesForKey(playRunner.playId);
                int i = 0;
                while (playLines.MoveNext())
                {
                    if (newPlayRunner.lineId == i)
                    {
                        nextLine = playLines.Current;
                        break;
                    }
                    i++;
                }

                newPlayRunner.stageId = playRunner.stageId;
                newPlayRunner.playId = playRunner.playId;
                newPlayRunner.timeLineUpdated = time;
                newPlayRunner.lineTime = 0;
                newPlayRunner.lineTimeMax = nextLine.life;

                ecb.SetComponent(entityInQueryIndex, entity, newPlayRunner);
                ecb.SetComponent(entityInQueryIndex, entity, nextLine);

                // Create dialogue requests
                var speaker = 0;
                if (nextLine.speaker == 0) { speaker = actors.alpha; }
                if (nextLine.speaker == 1) { speaker = actors.beta; }
                if (nextLine.speaker == 2) { speaker = actors.gamma; }

                var dr = new DialogueRequest
                {
                    actorId = speaker,
                    dialogueId = nextLine.dialogueId,
                    sent = 0
                };

                ecb.AppendToBuffer(entityInQueryIndex, entity, dr);
            }

            ecb.SetBuffer<PlayLineRequest>(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);
    }
}

public struct NeedsPlay : IComponentData { }

public struct PlayRunner : IComponentData
{
    public int stageId;
    public int playId;
    public int lineId;
    public double timeLineUpdated;
    public double lineTime;
    public double lineTimeMax;
}

public struct Line : IComponentData
{
    public int dialogueId;
    public int speaker;
    public int childA;
    public int childB;
    public int childC;
    public int childD;
    public bool isEnd;
    public int endingId;
    public float life;
}

public struct PlayLineRequest : IBufferElementData
{
    public int newLine;
}