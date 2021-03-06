using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class StartPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    public NativeMultiHashMap<int, Line> PlayLibrary
        = new NativeMultiHashMap<int, Line>(G.numberOfPlays * G.linesPerPlay, Allocator.Persistent);

    protected override void OnDestroy()
    {
        Dependency.Complete();
        PlayLibrary.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().AsParallelWriter();
        var playLibrary = PlayLibrary;

        var bestPlays = new NativeHashMap<int, PotentialPlay>(G.numberOfStages, Allocator.TempJob);
        var actorRoles = new NativeHashMap<int, PlayActorIds>(G.numberOfStages, Allocator.TempJob);

        // Build list of best plays per stage
        Entities
        .ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in Situation situation,
            in DynamicBuffer<PotentialPlay> validPlays,
            in PlayActorIds actors) =>
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

        // Start a play on a stage 
        Entities
        .WithNone<Line>()
        .ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in PlayRunner playRunner,
            in NeedsPlay needsPlay) =>
        {
            var wtf = new NativeMultiHashMapIterator<int>();
            var line = new Line();
            var pp = new PotentialPlay();

            if (!bestPlays.TryGetValue(playRunner.stageId, out pp)) return;

            playLibrary.TryGetFirstValue(pp.playId, out line, out wtf);

            var newPlayRunner = new PlayRunner
            {
                stageId = playRunner.stageId,
                playId = bestPlays[playRunner.stageId].playId,
                lineId = 1,
                timeLineUpdated = 0,
                lineTime = 0,
                lineTimeMax = line.life
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