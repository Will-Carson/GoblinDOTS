using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class FinishSituation : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().ToConcurrent();

        Entities
        .WithNone<NeedsNumberOfActors, NeedsRelationshipType>()
        .ForEach((Entity entity, int entityInQueryIndex, PartialSituation situation, DynamicBuffer<SituationParameters> parameters) =>
        {
            ecb.AddComponent<Situation>(entityInQueryIndex, entity);
            ecb.AddBuffer<PotentialPlay>(entityInQueryIndex, entity);
            ecb.RemoveComponent<PartialSituation>(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        ESECBS.AddJobHandleForProducer(Dependency);
    }
}