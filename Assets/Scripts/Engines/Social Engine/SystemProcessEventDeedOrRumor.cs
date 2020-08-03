using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;
using System;
using UnityEngine;

[ServerWorld]
[UpdateBefore(typeof(TransformSystemGroup))]
public class SystemProcessDeedOrRumorEvent : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    public NativeArray<DataDeed> DeedLibrary 
        = new NativeArray<DataDeed>(G.numberOfDeeds, Allocator.Persistent);

    protected override void OnCreate()
    {
        // Add deed example
        DeedLibrary[0] = new DataDeed() { values = new DataValues() { /* Define deed here */ } };
    }

    protected override void OnUpdate()
    {
        var buffer = ESECBS.CreateCommandBuffer();
        var deedLibrary = DeedLibrary;

        Entities
            .ForEach((Entity entity, FactionMember factionMember, Faction faction, DynamicBuffer<Relationship> relationships, DynamicBuffer<Memory> memories, DynamicBuffer<EventWitness> eventsWitness) =>
            {
                for (int i = 0; i < eventsWitness.Length; i++)
                {
                    #region data prep

                    var eventWitness = eventsWitness[i];
                    if (!eventWitness.needsEvaluation) return;

                    var newMemory = new Memory();
                    newMemory.rumorSpreaderFactionMember = eventWitness.rumorSpreaderfactionMember;
                    newMemory.deedDoerFactionMember = eventWitness.deedDoerfactionMember;
                    newMemory.type = eventWitness.type;
                    newMemory.deedTargetFactionMember = eventWitness.deedTargetfactionMember;
                    newMemory.timesCommitted = 1;
                    newMemory.reliability = eventWitness.reliability;

                    float gossipAffinity = 0;
                    float deedTargetAffinity = 0;
                    float deedDoerAffinity = 0;

                    for (int j = 0; j < relationships.Length; j++)
                    {
                        if (relationships[j].targetFaction.id == eventWitness.rumorSpreaderfactionMember.faction.id)
                            gossipAffinity = relationships[j].affinity;
                        if (relationships[j].targetFaction.id == eventWitness.deedTargetfactionMember.faction.id)
                            deedTargetAffinity = relationships[j].affinity;
                        if (relationships[j].targetFaction.id == eventWitness.deedDoerfactionMember.faction.id)
                            deedDoerAffinity = relationships[j].affinity;
                    }

                    var deedData = deedLibrary[(int)newMemory.type];
                    var deedValues = DataValues.GetValues(Allocator.TempJob, deedData.values);
                    float traitAlignment = 1;

                    // Get the difference between the character and the deeds traits. Will produce a number between 0 and 1.
                    // 0 being the least possible alignment and 1 being the most possible alignment.
                    var traits = DataValues.GetValues(Allocator.TempJob, faction.values);
                    for (int j = 0; j < traits.Length; j++)
                    {
                        traitAlignment -= abs(traits[j] - deedValues[j]);
                    }
                    traitAlignment = traitAlignment / deedValues.Length / 2;

                    // Dispose
                    deedValues.Dispose();
                    traits.Dispose();

                    if (eventWitness.rumorSpreaderfactionMember.id != factionMember.id)
                    {
                        newMemory.reliability = gossipAffinity * eventWitness.reliability;
                    }
                    else
                    {
                        newMemory.reliability = 1;
                    }

                    #endregion

                    #region affinity calculation

                    float affinityDelta = 0;

                    // Calculate affinity change from witness relationship to target
                    affinityDelta
                        += deedTargetAffinity
                        * newMemory.impact
                        * newMemory.reliability;
                    affinityDelta
                        += abs(affinityDelta)
                        * traitAlignment
                        * G.traitAlignmentImportance;
                    affinityDelta
                        += deedDoerAffinity
                        * factionMember.mood.arousal
                        * G.arrousalImportance;

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
                                        = tempRelationship.affinity
                                        + affinityDelta
                                        * pow(.8f, newMemory.timesCommitted) // TODO am I supposed to multiply by affinityDelta again? Seems wrong
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
                            targetFaction = newMemory.deedDoerFactionMember.faction,
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
                        * GetPowerCurve((factionMember.power
                        - newMemory.deedDoerFactionMember.power));

                    var tempFactionMember = factionMember;
                    tempFactionMember.mood = new Mood()
                    {
                        pleasure = tempFactionMember.mood.pleasure + pleasureDelta,
                        arousal = tempFactionMember.mood.arousal + arousalDelta,
                        dominance = tempFactionMember.mood.dominance + dominanceDelta
                    };

                    factionMember = tempFactionMember;

                    #endregion

                    eventWitness.needsEvaluation = false;
                }

                // Wipe buffer after going through all elements
                buffer.SetBuffer<EventWitness>(entity);
            })
            .WithBurst()
            .Run();
    }
    
    private static float GetPowerCurve(float x)
    {
        var a = 10;
        var result = pow(a, x) - 1;
        result = result / (a - 1);
        return result;
    }

    protected override void OnDestroy()
    {
        DeedLibrary.Dispose();
    }
}