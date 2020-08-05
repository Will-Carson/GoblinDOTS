using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class RunPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    public NativeMultiHashMap<int, Line> PlayLibrary 
        = new NativeMultiHashMap<int, Line>(G.numberOfPlays * G.linesPerPlay, Allocator.Persistent);

    protected override void OnDestroy()
    {
        Dependency.Complete();
        PlayLibrary.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().ToConcurrent();
        var playLibrary = PlayLibrary;
        var time = Time.ElapsedTime;

        // Increment lines and generate step requests
        Entities
        .WithNone<NeedsPlay>()
        .WithAll<Line>()
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            PlayRunner playRunner) =>
        {
            // generate step request if time is up for running line
            if (playRunner.lineTime > playRunner.lineTimeMax)
            {
                var p = new PlayLineRequest
                {
                    newLine = 0
                };
                ecb.AddComponent(entityInQueryIndex, entity, p);
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
            Entity entity,
            int entityInQueryIndex,
            PlayRunner playRunner,
            Line playingLine,
            PlayLineRequest newLine,
            PlayActorIds actors) =>
        {
            // Release stages for new plays 
            ecb.RemoveComponent<PlayLineRequest>(entityInQueryIndex, entity);
            if (playingLine.isEnd)
            {
                ecb.SetComponent(entityInQueryIndex, entity, new PlayRunner { stageId = playRunner.stageId });
                ecb.RemoveComponent<Line>(entityInQueryIndex, entity);
                ecb.AddComponent<NeedsPlay>(entityInQueryIndex, entity);
                return;
            }

            // Process step requests
            var newPlayRunner = new PlayRunner();
            if (newLine.newLine == 0) { newPlayRunner.lineId = playingLine.childA; }
            if (newLine.newLine == 1) { newPlayRunner.lineId = playingLine.childB; }
            if (newLine.newLine == 2) { newPlayRunner.lineId = playingLine.childC; }
            if (newLine.newLine == 3) { newPlayRunner.lineId = playingLine.childD; }

            var playLines = playLibrary.GetValuesForKey(playRunner.playId);

            var nextLine = new Line();
            int i = 0;
            while (playLines.MoveNext())
            {
                i++;
                nextLine = playLines.Current;
                if (newPlayRunner.lineId == i) { break; }
            }

            newPlayRunner.lineTime = 0;
            newPlayRunner.lineTimeMax = nextLine.life;
            newPlayRunner.playId = playRunner.playId;
            newPlayRunner.stageId = playRunner.stageId;
            newPlayRunner.timeLineUpdated = time;

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
                dialogueId = nextLine.dialogueId
            };

            ecb.AppendToBuffer(entityInQueryIndex, entity, dr);
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
    public float life;
}

public struct PlayLineRequest : IComponentData
{
    public int newLine;
}