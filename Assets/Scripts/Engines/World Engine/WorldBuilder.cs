using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using DOTSNET;
using System.Collections.Generic;

[ServerWorld]
public class WorldBuilder : MonoBehaviour
{
    private DialogueRequestApplier dialogueRequestApplier;
    private ProcessDeedOrRumorEvent processDeedOrRumorEvent;

    private EntityArchetype actor;
    private EntityArchetype stage;

    private int nextActorId = 0;
    private int nextFactionId = 0;
    private int nextFactionMemberId = 0;

    private int nextStageId = 0;

    private World server;
    private EntityManager em;

    void Start()
    {
        var worlds = World.All;
        foreach (var w in worlds)
        {
            if (w.Name == "ServerWorld") { server = w; }
        }

        dialogueRequestApplier = server.GetOrCreateSystem<DialogueRequestApplier>();
        processDeedOrRumorEvent = server.GetOrCreateSystem<ProcessDeedOrRumorEvent>();

        em = server.EntityManager;
        actor = em.CreateArchetype(new ComponentType[]
        {
            // Drama Engine
            typeof(ActorId),
            typeof(StageOccupant),
            typeof(ActorRelationship),
            // DOTSNET / Networking
            typeof(NetworkEntity),
            typeof(DialogueRequest),
            typeof(NetworkObserver),
            typeof(RebuildNetworkObserver),
            // Social Engine
            typeof(Relationship),
            typeof(Memory),
            typeof(WitnessedEvent),
            typeof(FactionMember),
            // Translation
            typeof(Translation),
            typeof(Rotation)
        });
        stage = em.CreateArchetype(new ComponentType[] 
        {
            typeof(StageId),
            typeof(PlayRunner),
            typeof(PlayActorIds),
            typeof(Exhausted),
            typeof(Occupant),
            typeof(DialogueRequest),
            typeof(PlayConsiquence),
            typeof(FindDeedWitnessesRequest),
            typeof(PlayLineRequest),
            typeof(NeedsPlay)
        });

        // TODO TEST
        
        var e = em.CreateEntity();
        var r = new BuildSituationRequest { stageId = 0 };
        em.AddComponent<BuildSituationRequest>(e);
        em.SetComponentData(e, r);

        var numOfStages = 3;
        var numOfActors = 10;

        var stages = new Dictionary<int, Entity>();

        for (int i = 0; i < numOfStages; i++)
        {
            var s = CreateStage();
            var bo = em.GetBuffer<Occupant>(s);
            stages.Add(i, s);
        }

        for (int i = 0; i < numOfActors; i++)
        {
            var s = Random.Range(0, numOfStages);
            var a = CreateActor(s);
            var relationshipBuffer = em.GetBuffer<ActorRelationship>(a);
            relationshipBuffer.Add(new ActorRelationship { owner = 0, target = 1, type = 0 });

            var occupantBuffer = em.GetBuffer<Occupant>(stages[s]);
            occupantBuffer.Add(new Occupant { id = i, occupant = a });
        }
    }

    public Entity CreateActor(int stageId)
    {
        var a = em.CreateEntity(actor);
        var fmd = new FactionMemberData
        {
            id = nextFactionMemberId,
            faction = new Faction { id = nextFactionId }
        };
        processDeedOrRumorEvent.FactionMembers.TryAdd(nextFactionMemberId, fmd);

        em.SetComponentData(a, new ActorId { value = nextActorId });
        em.SetComponentData(a, new StageOccupant { stageId = stageId });
        dialogueRequestApplier.Actors.TryAdd(nextActorId, a);

        nextActorId++;
        nextFactionId++;
        nextFactionMemberId++;
        return a;
    }

    public Entity CreateStage()
    {
        var a = em.CreateEntity(stage);
        em.SetComponentData(a, new StageId { value = nextStageId });
        em.SetComponentData(a, new PlayRunner { stageId = nextStageId });
        nextStageId++;
        return a;
    }
}

public struct BuildActorRequest : IComponentData
{
    public int stageId;
}

public struct BuildStageRequest : IComponentData
{

}