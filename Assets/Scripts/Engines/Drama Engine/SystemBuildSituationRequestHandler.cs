using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class SystemBuildSituationRequestHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    private EntityArchetype SituationBuilder;

    protected override void OnCreate()
    {
        var plainBuffer = ESECBS.CreateCommandBuffer();
        SituationBuilder = EntityManager.CreateArchetype(new ComponentType[]
        {
            typeof(PartialSituation),
            typeof(StageParameters),
            typeof(NeedsNumberOfActors),
            typeof(NeedsRelationshipType)
        });

        // TODO TEST

        var e = plainBuffer.CreateEntity();
        var r = new BuildSituationRequest { stageId = 0 };
        plainBuffer.AddComponent(e, r);

        var actor1 = plainBuffer.CreateEntity();
        var actorId = new ActorId { value = 0 };
        var stageOccupant = new StageOccupant { stageId = 0 };
        var relationships = plainBuffer.AddBuffer<ActorRelationship>(actor1);
        var relationship = new ActorRelationship { owner = 0, target = 1, type = 0 };
        plainBuffer.AddComponent(actor1, actorId);
        plainBuffer.AddComponent(actor1, stageOccupant);
        plainBuffer.AppendToBuffer(actor1, relationship);

        var actor2 = plainBuffer.CreateEntity();
        actorId = new ActorId { value = 1 };
        relationships = plainBuffer.AddBuffer<ActorRelationship>(actor2);
        relationship = new ActorRelationship { owner = 1, target = 0, type = 0 };
        plainBuffer.AddComponent(actor2, actorId);
        plainBuffer.AddComponent(actor2, stageOccupant);
        plainBuffer.AppendToBuffer(actor2, relationship);

        e = plainBuffer.CreateEntity();
        var stageId = new StageId { value = 0 };
        plainBuffer.AddComponent(e, stageId);
        var b = plainBuffer.AddBuffer<Occupant>(e);
        var occupant = new Occupant { id = 0, occupant = actor1 };
        plainBuffer.AppendToBuffer(e, occupant);
        occupant = new Occupant { id = 0, occupant = actor2 };
        plainBuffer.AppendToBuffer(e, occupant);
        var playRunner = new PlayRunner { stageId = 0 };
        plainBuffer.AddComponent(e, playRunner);
    }

    protected override void OnDestroy()
    {

    }

    protected override void OnUpdate()
    {
        var buffer = ESECBS.CreateCommandBuffer().ToConcurrent();
        var situationBuilder = SituationBuilder;

        // Not sure if entityInQueryIndex will work for jobchunk id.
        Entities.ForEach((Entity entity, int entityInQueryIndex, BuildSituationRequest request) =>
        {
            var e = buffer.CreateEntity(entityInQueryIndex, situationBuilder);
            var s = new Situation();
            s.stageId = request.stageId;
            buffer.AddComponent(entityInQueryIndex, e, s);
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