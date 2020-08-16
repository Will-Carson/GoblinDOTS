using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using DOTSNET;

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

        var actor1 = CreateActor(0);
        var b = em.GetBuffer<ActorRelationship>(actor1);
        b.Add(new ActorRelationship { owner = 0, target = 1, type = 0 });

        var actor2 = CreateActor(0);
        b = em.GetBuffer<ActorRelationship>(actor2);
        b.Add(new ActorRelationship { owner = 1, target = 0, type = 0 });

        var actor3 = CreateActor(0);

        e = em.CreateEntity(stage);
        var bo = em.GetBuffer<Occupant>(e);
        bo.Add(new Occupant { id = 0, occupant = actor1 });
        bo.Add(new Occupant { id = 1, occupant = actor2 });
        bo.Add(new Occupant { id = 2, occupant = actor3 });
    }

    public Entity CreateActor(int stageId)
    {
        var a = em.CreateEntity(actor);
        var fm = new FactionMember
        {
            id = nextFactionMemberId,
            faction = new Faction { id = nextFactionId }
        };
        processDeedOrRumorEvent.FactionMembers.TryAdd(nextFactionMemberId, fm);

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