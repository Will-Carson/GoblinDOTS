using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class StartPlay : SystemBase
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

        var bestPlays = new NativeHashMap<int, PotentialPlay>(G.numberOfStages, Allocator.TempJob);
        var actorRoles = new NativeHashMap<int, PlayActorIds>(G.numberOfStages, Allocator.TempJob);

        // Build list of best plays per stage
        Entities
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            Situation situation,
            DynamicBuffer<PotentialPlay> validPlays,
            PlayActorIds actors) =>
        {
            for (int i = 0; i < validPlays.Length; i++)
            {
                if (!bestPlays.ContainsKey(situation.stageId) || validPlays[i].drama > bestPlays[situation.stageId].drama)
                {
                    bestPlays.Add(situation.stageId, validPlays[i]);
                    actorRoles.Add(situation.stageId, actors);
                }
            }
            ecb.AddComponent<Garbage>(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        // TODO Might not be needed
        Dependency.Complete();

        // Start a play on a stage 
        Entities
        .WithNone<Line>()
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            PlayRunner playRunner,
            NeedsPlay needsPlay) =>
        {
            var wtf = new NativeMultiHashMapIterator<int>();
            var line = new Line();
            var pp = new PotentialPlay();

            if (!bestPlays.TryGetValue(playRunner.stageId, out pp)) return;

            playLibrary.TryGetFirstValue(pp.playId, out line, out wtf);

            var newPlayRunner = new PlayRunner
            {
                playId = bestPlays[playRunner.stageId].playId,
                stageId = playRunner.stageId,
                lineTimeMax = line.life,
                lineId = 0,
                lineTime = 0,
                timeLineUpdated = 0
            };

            var actors = actorRoles[playRunner.stageId];

            ecb.AddComponent(entityInQueryIndex, entity, actors);
            ecb.AddComponent(entityInQueryIndex, entity, line);
            ecb.SetComponent(entityInQueryIndex, entity, newPlayRunner);
            ecb.RemoveComponent<NeedsPlay>(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);
        bestPlays.Dispose();
        actorRoles.Dispose();
    }
}