using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class SystemBuildSituationRequestHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    private EntityArchetype situationBuilder;

    protected override void OnCreate()
    {
        situationBuilder = EntityManager.CreateArchetype(new ComponentType[]
        {
            typeof(PartialSituation),
            typeof(ParameterBuffer),
            typeof(NeedsNumberOfActors),
            typeof(NeedsRelationshipType)
        });
    }

    protected override void OnDestroy()
    {

    }

    private EntityCommandBuffer.Concurrent buffer;

    protected override void OnUpdate()
    {
        buffer = ESECBS.CreateCommandBuffer().ToConcurrent();

        // Not sure if entityInQueryIndex will work for jobchunk id.
        Entities.ForEach((Entity entity, int entityInQueryIndex, BuildSituationRequest request) =>
        {
            var e = buffer.CreateEntity(entityInQueryIndex, situationBuilder);
            var s = new Situation();
            s.stageId = request.stageId;
            buffer.SetComponent(entityInQueryIndex, e, s);
        })
        .WithBurst()
        .Schedule();
    }
}

public struct BuildSituationRequest : IComponentData
{
    public int stageId;
}

public struct PartialSituation : IComponentData { public int stageId; }

public struct NeedsNumberOfActors : IComponentData { }
public struct NeedsRelationshipType : IComponentData { }