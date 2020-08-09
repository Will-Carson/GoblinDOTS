using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class BuildSituationRequestHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;
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

        // TODO TEST

        var e = pecb.CreateEntity();
        var r = new BuildSituationRequest { stageId = 0 };
        pecb.AddComponent(e, r);

        var actor1 = pecb.CreateEntity();
        var actorId = new ActorId { value = 0 };
        var stageOccupant = new StageOccupant { stageId = 0 };
        var relationships = pecb.AddBuffer<ActorRelationship>(actor1);
        var relationship = new ActorRelationship { owner = 0, target = 1, type = 0 };
        pecb.AddComponent(actor1, actorId);
        pecb.AddComponent(actor1, stageOccupant);
        pecb.AppendToBuffer(actor1, relationship);

        var actor2 = pecb.CreateEntity();
        actorId = new ActorId { value = 1 };
        relationships = pecb.AddBuffer<ActorRelationship>(actor2);
        relationship = new ActorRelationship { owner = 1, target = 0, type = 0 };
        pecb.AddComponent(actor2, actorId);
        pecb.AddComponent(actor2, stageOccupant);
        pecb.AppendToBuffer(actor2, relationship);

        var actor3 = pecb.CreateEntity();
        actorId = new ActorId { value = 2 };
        relationships = pecb.AddBuffer<ActorRelationship>(actor3);
        //relationship = new ActorRelationship { owner = 1, target = 0, type = 0 };
        pecb.AddComponent(actor3, actorId);
        pecb.AddComponent(actor3, stageOccupant);
        //plainBuffer.AppendToBuffer(actor3, relationship);

        e = pecb.CreateEntity();
        var stageId = new StageId { value = 0 };
        pecb.AddComponent(e, stageId);
        var b = pecb.AddBuffer<Occupant>(e);
        var occupant = new Occupant { id = 0, occupant = actor1 };
        pecb.AppendToBuffer(e, occupant);
        occupant = new Occupant { id = 1, occupant = actor2 };
        pecb.AppendToBuffer(e, occupant);
        occupant = new Occupant { id = 2, occupant = actor3 };
        pecb.AppendToBuffer(e, occupant);
        var playRunner = new PlayRunner { stageId = 0 };
        pecb.AddComponent(e, playRunner);
        var needsPlay = new NeedsPlay();
        pecb.AddComponent(e, needsPlay);
        pecb.AddBuffer<DialogueRequest>(e);
        pecb.AddBuffer<NetworkObserver>(e);
        pecb.AddBuffer<RebuildNetworkObserver>(e);
        pecb.AddBuffer<PlayLineRequest>(e);
        pecb.AddComponent<Translation>(e);
        pecb.AddComponent<Rotation>(e);
        pecb.AddComponent<NetworkEntity>(e);
        pecb.AddComponent<NetworkTransform>(e);
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