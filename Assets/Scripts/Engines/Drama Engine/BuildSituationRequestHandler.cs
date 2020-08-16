using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class BuildSituationRequestHandler : SystemBase
{
    [AutoAssign] protected EndSimulationEntityCommandBufferSystem ESECBS;
    private EntityArchetype SituationBuilder;

    protected override void OnCreate()
    {
        var pecb = ESECBS.CreateCommandBuffer();
        SituationBuilder = EntityManager.CreateArchetype(new ComponentType[]
        {
            typeof(PartialSituation),
            typeof(SituationParameters),
            typeof(NeedsNumberOfActors),
            typeof(NeedsRelationshipType)
        });
    }

    protected override void OnDestroy()
    {

    }

    protected override void OnUpdate()
    {
        var buffer = ESECBS.CreateCommandBuffer().AsParallelWriter();
        var situationBuilder = SituationBuilder;

        // Not sure if entityInQueryIndex will work for jobchunk id.
        Entities.ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in BuildSituationRequest request) =>
        {
            var e = buffer.CreateEntity(entityInQueryIndex, situationBuilder);
            var s = new PartialSituation();
            s.stageId = request.stageId;
            buffer.SetComponent(entityInQueryIndex, e, s);
            buffer.DestroyEntity(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        ESECBS.AddJobHandleForProducer(Dependency);
    }
}

public struct BuildSituationRequest : IComponentData { public int stageId; }
public struct PartialSituation : IComponentData { public int stageId; }

public struct NeedsNumberOfActors : IComponentData { }
public struct NeedsRelationshipType : IComponentData { }