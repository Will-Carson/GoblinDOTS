using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class SystemFinishSituation : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    private EntityCommandBuffer.Concurrent Buffer;

    protected override void OnCreate()
    {
        Buffer = ESECBS.CreateCommandBuffer().ToConcurrent();
    }

    protected override void OnUpdate()
    {
        var buffer = Buffer;
        
        Entities.WithNone<NeedsNumberOfActors, NeedsRelationshipType>().
        ForEach((Entity entity, int entityInQueryIndex, PartialSituation situation, DynamicBuffer<StageParameters> parameters) =>
        {
            buffer.RemoveComponent(entityInQueryIndex, entity, typeof(PartialSituation));
            buffer.AddComponent(entityInQueryIndex, entity, typeof(Situation));
            buffer.AddComponent(entityInQueryIndex, entity, typeof(ValidPlayId));
            buffer.AddComponent(entityInQueryIndex, entity, typeof(StageParameters));
        })
        .WithBurst()
        .Schedule();
    }
}