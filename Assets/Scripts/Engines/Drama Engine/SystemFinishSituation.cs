using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class SystemFinishSituation : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    private EntityCommandBuffer.Concurrent buffer;

    protected override void OnUpdate()
    {
        buffer = ESECBS.CreateCommandBuffer().ToConcurrent();
        
        Entities.WithNone<NeedsNumberOfActors, NeedsRelationshipType>().
        ForEach((Entity entity, int entityInQueryIndex, PartialSituation situation, DynamicBuffer<ParameterBuffer> parameters) =>
        {
            buffer.RemoveComponent(entityInQueryIndex, entity, typeof(PartialSituation));
            buffer.AddComponent(entityInQueryIndex, entity, typeof(Situation));
            buffer.AddComponent(entityInQueryIndex, entity, typeof(ValidPlayId));
            buffer.AddComponent(entityInQueryIndex, entity, typeof(ParameterBuffer));
        })
        .WithBurst()
        .Schedule();
    }
}