using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class DialogueRequestApplier : SystemBase
{
    [AutoAssign] protected EndSimulationEntityCommandBufferSystem ESECBS;
    public NativeHashMap<int, Entity> Actors 
        = new NativeHashMap<int, Entity>(G.maxTotalPopulation, Allocator.Persistent);

    protected override void OnDestroy()
    {
        Actors.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().ToConcurrent();
        var actors = Actors;

        // Rebuild actor list
        Entities
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            ActorId actorId) =>
        {
            actors.TryAdd(actorId.value, entity);
        })
        .WithBurst()
        .Schedule();

        // Distribute DialogueRequests from stages to actors
        Entities
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            StageId stageId,
            DynamicBuffer<DialogueRequest> requests) =>
        {
            for (int i = 0; i < requests.Length; i++)
            {
                var dr = new DialogueRequest
                {
                    actorId = requests[i].actorId,
                    dialogueId = requests[i].dialogueId,
                    sent = 0
                };
                ecb.AppendToBuffer(entityInQueryIndex, actors[dr.actorId], dr);
                requests.RemoveAt(i);
            }
        })
        .WithBurst()
        .Schedule();

        ESECBS.AddJobHandleForProducer(Dependency);
    }
}
