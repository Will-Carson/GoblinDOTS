using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;
using System.Collections.Generic;

[ServerWorld]
public class ProcessDeedOrRumorEvent : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    public NativeArray<DataDeed> DeedLibrary 
        = new NativeArray<DataDeed>(G.numberOfDeeds, Allocator.Persistent);
    public NativeHashMap<int, FactionMember> FactionMembers 
        = new NativeHashMap<int, FactionMember>();

    protected override void OnCreate()
    {
        // Add deed example
        DeedLibrary[0] = new DataDeed() { values = new DataValues() { /* Define deed here */ } };
    }

    protected override void OnDestroy()
    {
        DeedLibrary.Dispose();
        FactionMembers.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer();
        var deedLibrary = DeedLibrary;

        var factionMembers = FactionMembers;

        Entities
            .ForEach((
                ref DynamicBuffer<Relationship> relationships,
                ref DynamicBuffer<Memory> memories,
                ref DynamicBuffer<WitnessedEvent> witnessedEvents,
                in Entity entity, 
                in FactionMember factionMember) =>
            {
                for (int i = 0; i < witnessedEvents.Length; i++)
                {
                    #region data prep

                    var witnessedEvent = witnessedEvents[i];
                    if (!witnessedEvent.needsEvaluation) return;

                    var newMemory = new Memory();
                    newMemory.rumorSpreaderFactionMemberId = witnessedEvent.rumorSpreaderFMId;
                    newMemory.deedDoerFactionMemberId = witnessedEvent.deedDoerFMId;
                    newMemory.type = witnessedEvent.type;
                    newMemory.deedTargetFactionMemberId = witnessedEvent.deedTargetFMId;
                    newMemory.timesCommitted = 1;
                    newMemory.reliability = witnessedEvent.reliability;

                    int gossipAffinity = 0;
                    int deedTargetAffinity = 0;
                    int deedDoerAffinity = 0;

                    var spreader = new FactionMember();
                    var target = new FactionMember();
                    var doer = new FactionMember();

                    factionMembers.TryGetValue(witnessedEvents[i].rumorSpreaderFMId, out spreader);
                    factionMembers.TryGetValue(witnessedEvents[i].deedTargetFMId, out target);
                    factionMembers.TryGetValue(witnessedEvents[i].deedDoerFMId, out doer);

                    for (int j = 0; j < relationships.Length; j++)
                    {
                        if (relationships[j].targetFaction.id == spreader.faction.id)
                            gossipAffinity = relationships[j].affinity;
                        if (relationships[j].targetFaction.id == target.faction.id)
                            deedTargetAffinity = relationships[j].affinity;
                        if (relationships[j].targetFaction.id == doer.faction.id)
                            deedDoerAffinity = relationships[j].affinity;
                    }

                    var deedData = deedLibrary[(int)newMemory.type];
                    var deedValues = DataValues.GetValues(Allocator.TempJob, deedData.values);
                    float traitAlignment = 1;

                    // Get the difference between the character and the deeds traits. Will produce a number between 0 and 1.
                    // 0 being the least possible alignment and 1 being the most possible alignment.
                    var faction = factionMember.faction;
                    var traits = DataValues.GetValues(Allocator.TempJob, faction.values);
                    for (int j = 0; j < traits.Length; j++)
                    {
                        traitAlignment -= abs(traits[j] - deedValues[j]);
                    }
                    traitAlignment = traitAlignment / deedValues.Length / 2;

                    // Dispose
                    deedValues.Dispose();
                    traits.Dispose();

                    if (witnessedEvent.rumorSpreaderFMId != factionMember.id)
                    {
                        newMemory.reliability = gossipAffinity * witnessedEvent.reliability;
                    }
                    else
                    {
                        newMemory.reliability = 1;
                    }

                    #endregion

                    #region affinity calculation

                    int affinityDelta = 0;

                    // Calculate affinity change from witness relationship to target
                    affinityDelta
                        += (int)(deedTargetAffinity
                        * newMemory.impact
                        * newMemory.reliability);
                    affinityDelta
                        += (int)(abs(affinityDelta)
                        * traitAlignment
                        * G.traitAlignmentImportance);
                    affinityDelta
                        += (int)(deedDoerAffinity
                        * factionMember.mood.arousal
                        * G.arrousalImportance);

                    var isEstablished = false;

                    for (int j = 0; j < relationships.Length; j++)
                    {
                        if (relationships[j].targetFaction.id == faction.id)
                        {
                            // Update relationship
                            isEstablished = true;
                            var tempRelationship = relationships[j];
                            relationships.RemoveAt(j);
                            var updatedRelationship = new Relationship()
                            {
                                targetFaction = tempRelationship.targetFaction,
                                affinity
                                        = (int)(tempRelationship.affinity
                                        + affinityDelta
                                        * pow(.8f, newMemory.timesCommitted)) // TODO am I supposed to multiply by affinityDelta again? Seems wrong
                            };
                            newMemory.impact
                                = affinityDelta
                                * pow(.8f, newMemory.timesCommitted); // TODO am I supposed to multiply by affinityDelta again? Seems wrong
                        }
                    }

                    if (!isEstablished)
                    {
                        var relationship = new Relationship()
                        {
                            targetFaction = doer.faction,
                            affinity = affinityDelta
                        };

                        relationships.Add(relationship);
                    }

                    #endregion

                    #region handle memory

                    float lowestMemoryImpact = 1;
                    var lowestImpactMemory = 0;
                    if (memories.Length >= G.memoriesPerCharacter)
                    {
                        for (var j = 0; j < memories.Length; j++)
                        {
                            lowestMemoryImpact = memories[j].impact;
                            lowestImpactMemory = j;
                        }

                        if (lowestMemoryImpact < newMemory.impact)
                        {
                            memories.RemoveAt(lowestImpactMemory);
                            memories.Add(newMemory);
                        }
                    }
                    else memories.Add(newMemory);

                    #endregion

                    #region adjust factionMember mood

                    float pleasureDelta
                        = deedDoerAffinity
                        + (deedDoerAffinity
                        * traitAlignment);
                    float arousalDelta
                        = abs(deedDoerAffinity)
                        * .2f; // TODO magic number "arousal importance"
                    float dominanceDelta
                        = sign(newMemory.impact)
                        * sign(deedTargetAffinity)
                        * abs(deedData.Aggression)
                        * abs(affinityDelta);
                    dominanceDelta
                        += abs(dominanceDelta)
                        * GetPowerCurve(factionMember.power
                        - factionMembers[newMemory.deedDoerFactionMemberId].power);

                    var newFactionMember = factionMember;
                    newFactionMember.mood = new Mood()
                    {
                        pleasure = newFactionMember.mood.pleasure + (int)(pleasureDelta * 100),
                        arousal = newFactionMember.mood.arousal + (int)(arousalDelta * 100),
                        dominance = newFactionMember.mood.dominance + (int)(dominanceDelta * 100)
                    };
                    
                    ecb.SetComponent(entity, newFactionMember);

                    #endregion

                    witnessedEvent.needsEvaluation = false;
                    witnessedEvents.RemoveAt(i);
                }
            })
            .WithBurst()
            .Run();

        ESECBS.AddJobHandleForProducer(Dependency);
    }
    
    private static float GetPowerCurve(float x)
    {
        var a = 10;
        var result = pow(a, x) - 1;
        result = result / (a - 1);
        return result;
    }
}

public struct Memory : IBufferElementData
{
    public int rumorSpreaderFactionMemberId;
    public int deedDoerFactionMemberId;
    public DeedType type;
    public int deedTargetFactionMemberId;
    public int timesCommitted;
    public float impact;
    public float reliability;
}

public struct Relationship : IBufferElementData
{
    public Faction targetFaction;
    public int affinity;
    public RelationshipValues values;
}

public struct RelationshipValues
{
    // TODO BROKE
}

public struct WitnessedEvent : IBufferElementData
{
    public int rumorSpreaderFMId;
    public int deedDoerFMId;
    public DeedType type;
    public int deedTargetFMId;
    public int deedWitnessFactionMemberId;
    public int reliability;
    public bool isRumor;
    public bool needsEvaluation;
}

public struct Mood 
{
    public int pleasure;
    public int arousal;
    public int dominance;
}

public struct DataDeed
{
    public DeedType Type { get; }
    public float Aggression { get; }
    public float Impact { get; }
    public DataValues values;
}

public struct DataValues
{
    // TODO BROKE
    public static NativeArray<int> GetValues(Allocator a, DataValues v)
    {
        return new NativeArray<int>(2, a);
    }
}

public struct FactionMember : IComponentData
{
    public int id;
    public Faction faction;
    public Mood mood;
    public int power;
}

public struct Faction
{
    public int id;
    public DataValues values;
}

public struct Witness : IBufferElementData
{
    public Entity witness;
}

public enum DeedType
{
    Betrayed,
    Reconciled,
    Robbed
}

public enum RelationshipType
{
    Brother,
    Sister,
    Father,
    Mother,
    Lover,
    Other
}

public enum RelationshipValueType
{
    Affinity
    // Respect,
    // Admiration,
    // Duty,
    // etc.
}

public enum ValueType
{
    // Charismatic,
    // Angry,
    // Caring,
    // etc.
}