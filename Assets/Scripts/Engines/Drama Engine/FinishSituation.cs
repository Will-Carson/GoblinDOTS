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
        var ecb = ESECBS.CreateCommandBuffer().AsParallelWriter();

        Entities
        .WithNone<NeedsNumberOfActors, NeedsRelationshipType>()
        .ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in PartialSituation situation,
            in DynamicBuffer<SituationParameters> parameters) =>
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