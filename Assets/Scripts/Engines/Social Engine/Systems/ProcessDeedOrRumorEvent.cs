using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class ProcessDeedOrRumorEvent : SystemBase
{
    public NativeHashMap<int, Deed> DeedLibrary = new NativeHashMap<int, Deed>();

    public NativeList<RumorEvent> RumorEvents = new NativeList<RumorEvent>();
    public NativeList<DeedEvent> DeedEvents = new NativeList<DeedEvent>();
    
    [AutoAssign] protected ManageFactionAndFactionMembers FactionSystem;

    public NativeHashMap<int, Relationship> Relationships = new NativeHashMap<int, Relationship>();
    public NativeHashMap<int, Memory> Memories = new NativeHashMap<int, Memory>();

    public NativeArray<int> NextRelationshipId;
    public NativeArray<int> NextMemoryId;

    protected override void OnCreate()
    {
        // TODO in the future these values should be pulled from a database instead of being set to 0
        // This will facilitate server restarts.
        NextRelationshipId[0] = 0;
        NextMemoryId[0] = 0;

        // Define deed library
        DeedLibrary = new NativeHashMap<int, Deed>();
        DeedLibrary.Add(0, new Deed()
        {
            // Add deed
        });
    }
    
    [BurstCompile]
    struct ProcessDeedOrRumorEventJob : IJob
    {
        public NativeHashMap<int, Deed> deedLibrary;
        public NativeHashMap<int, FactionMemberStruct> factionMembers;
        public NativeHashMap<int, Faction> factions;
        public NativeList<RumorEvent> rumorEvents;
        public NativeList<DeedEvent> deedEvents;
        public NativeHashMap<int, Relationship> relationships;
        public NativeHashMap<int, Memory> memories;
        public NativeArray<int> nextRelationshipId;
        public NativeArray<int> nextMemoryId;

        public void Execute()
        {
            for (int i = 0; i < rumorEvents.Length; i++)
            {
                // Process rumor events into new memories
                if (rumorEvents[i].needsEvaluation)
                {
                    for (int j = 0; j < rumorEvents[i].listeners.Length; j++)
                    {
                        ProcessRumor(rumorEvents[i].listeners[j], rumorEvents[i]);
                    }
                }
            }

            for (int i = 0; i < deedEvents.Length; i++)
            {
                // Process deed events into memories
                if (deedEvents[i].needsEvaluation)
                {
                    for (int j = 0; j < deedEvents[i].witnesses.Length; j++)
                    {
                        ProcessDeed(deedEvents[i].witnesses[j], deedEvents[i]);
                    }
                }
            }
        }

        private void WitnessDeed(DeedEvent de)
        {
            if (de.deedDoer == 0)
            {
                // Deed doer is invalid.
                return;
            }
            if (de.type == default)
            {
                // Deed is invalid.
                return;
            }
            
            for (int i = 0; i < de.witnesses.Length; i++)
            {
                ProcessDeed(de.witnesses[i], de);
            }
        }

        private void ProcessDeed(int witness, DeedEvent de)
        {
            var re = new RumorEvent()
            {
                needsEvaluation = false,
                rumorSpreader = witness,
                listeners = new int[] { witness },
                deedDoer = de.deedDoer,
                type = de.type,
                deedTarget = de.deedtarget,
                reliability = 1
            };
            ProcessRumor(witness, re);
        }

        private void ProcessRumor(int witness, RumorEvent re)
        {
            FactionMemberStruct fm = factionMembers[witness];
            Memory memory = new Memory();
            if (witness != re.rumorSpreader)
            {
                memory.reliability = GetReliability(witness, re);
            }

            memory.rumorSpreader = re.rumorSpreader;
            memory.deedDoer = re.deedDoer;
            memory.type = re.type;
            memory.deedTarget = re.deedTarget;
            memory.timesCommitted = 1;

            #region affinity calculation

            // Process the memory into their relationship
            float affinityDelta = 0;

            // Calculate affinity change from witness relationship to target
            affinityDelta += GetAffinity(witness, memory.deedTarget) * memory.impact * memory.reliability;
            affinityDelta += abs(affinityDelta) * GetTraitAlignment(witness, memory.type) * .2f; // TODO magic number "trait alignment importance"
            affinityDelta += GetAffinity(witness, memory.deedDoer) * fm.mood[1] * .2f; // TODO magic number "arousal importance

            var faction = factions[fm.factionId];
            var factionRelationshipIds = faction.relationshipIds;
            bool establishedRelationship = false;

            for (int i = 0; i < factionRelationshipIds.Count; i++)
            {
                var relationship = relationships[factionRelationshipIds[i]];
                if (relationship.targetId == memory.deedDoer)
                {
                    // Update relationship
                    relationship.relationshipValues[0] += affinityDelta * pow(.8f, memory.timesCommitted) * affinityDelta;
                    memory.impact = affinityDelta * pow(.8f, memory.timesCommitted) * affinityDelta;
                    establishedRelationship = true;
                }
            }

            if (!establishedRelationship)
            {
                Relationship relationship = new Relationship()
                {
                    targetId = memory.deedDoer,
                    relationshipValues = new float[]
                    {
                        affinityDelta
                    }
                };

                var relationshipId = GetNextRelationshipId();
                faction.relationshipIds.Add(relationshipId);
                relationships.Add(relationshipId, relationship);
            }

            #endregion

            #region add memory
            
            int memoryId = 0;

            // Add memory to memories
            // Check if the memory is a duplicate
            for (int i = 0; i < fm.memoryIds.Count; i++)
            {
                if (CheckDuplicateMemory(memories[fm.memoryIds[i]], memory))
                {
                    memoryId = fm.memoryIds[i];
                    memory.timesCommitted = memories[fm.memoryIds[i]].timesCommitted + 1;
                }
            }

            // If this character has too many memories, replace the lowest impact memory.
            // Only if the lowest impact memory is lower impact then the new memory.
            if (fm.memoryIds.Count > 10) // TODO magic number "max number of memories"
            {
                float lowestImpact = 1;
                int lowestImpactMemoryId = 0;
                for (int i = 0; i < fm.memoryIds.Count; i++)
                {
                    if (memories[fm.memoryIds[i]].impact < lowestImpact)
                    {
                        lowestImpact = memories[fm.memoryIds[i]].impact;
                        lowestImpactMemoryId = fm.memoryIds[i];
                    }
                }

                if (memory.impact < memories[lowestImpactMemoryId].impact)
                {
                    memoryId = lowestImpactMemoryId;
                }
            }

            // Otherwise, just give them the next memory ID.
            if (memoryId == 0) { memoryId = GetNextMemoryId(); }

            // Add the memory to the faction members memories, as well as the memories hashset
            memories.Add(memoryId, memory);
            fm.memoryIds.Add(memoryId);

            #endregion

            #region adjust mood

            // Process memory into faction member mood
            float pleasureDelta = GetAffinity(witness, memory.deedDoer) + (GetAffinity(witness, memory.deedDoer) * GetTraitAlignment(witness, memory.type));
            float arousalDelta = abs(GetAffinity(witness, memory.deedDoer)) * .2f; // TODO magic number "arousal importance"
            float dominanceDelta = sign(memory.impact) * sign(GetAffinity(witness, memory.deedTarget)) * abs(GetDeed(memory.type).Aggression) * abs(affinityDelta);
            dominanceDelta += abs(dominanceDelta) * GetPowerCurve(fm.power - factionMembers[memory.deedDoer].power);

            factionMembers[witness].mood[0] += pleasureDelta;
            factionMembers[witness].mood[1] += arousalDelta;
            factionMembers[witness].mood[2] += dominanceDelta;

            #endregion
        }

        private float GetPowerCurve(float xpos)
        {
            var a = 10;
            var result = pow(a, xpos) - 1;
            result = result / (a - 1);
            return result;
        }

        private bool CheckDuplicateMemory(Memory memory1, Memory memory2)
        {
            if (memory1.deedDoer == memory2.deedDoer &&
                memory1.deedTarget == memory2.deedTarget &&
                memory1.type == memory2.type)
            {
                return true;
            }
            return false;
        }

        private int GetNextMemoryId()
        {
            nextMemoryId[0]++;
            return nextMemoryId[0];
        }

        private int GetNextRelationshipId()
        {
            nextRelationshipId[0]++;
            return nextRelationshipId[0];
        }

        private float GetReliability(int witness, RumorEvent re)
        {
            return GetAffinity(witness, re.rumorSpreader) * re.reliability;
        }

        private float GetAffinity(int master, int target)
        {
            var faction = factions[factionMembers[master].factionId];
            if (!faction.relationshipIds.Contains(target)) { return 0; }
            return relationships[faction.relationshipIds[target]].relationshipValues[0];
        }

        private float GetTraitAlignment(int master, int deed)
        {
            var factionID = factionMembers[master].factionId;
            // Return if the faction is invalid.
            if (factionID == 0) { return 0; }
            var faction = factions[factionID];

            var deedTraits = GetDeedTraits(deed);
            float traitAlignment = 1;

            if (deedTraits.Length == 0)
            {
                // Deed traits are undefined.
                return 0;
            }

            if (deedTraits.Length < faction.personalityTraits.Length)
            {
                // Deed / faction trait # misalignment
                return 0;
            }

            // Get the difference between the character and the deeds traits. Will produce a number between 0 and 1.
            // 0 being the least possible alignment and 1 being the most possible alignment.
            for (int i = 0; i < faction.personalityTraits.Length; i++)
            {
                traitAlignment -= abs(faction.personalityTraits[i] - deedTraits[i]);
            }

            traitAlignment = traitAlignment / deedTraits.Length / 2;

            return traitAlignment;
        }

        private float[] GetDeedTraits(int deed)
        {
            return deedLibrary[deed].Values;
        }

        public Deed GetDeed(int deedId)
        {
            return deedLibrary[deedId];
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new ProcessDeedOrRumorEventJob()
        {
            deedLibrary = DeedLibrary,
            factionMembers = FactionSystem.FactionMembers,
            factions = FactionSystem.Factions,
            rumorEvents = RumorEvents,
            deedEvents = DeedEvents,
            relationships = Relationships,
            memories = Memories,
            nextMemoryId = NextMemoryId,
            nextRelationshipId = NextRelationshipId
        };

        Dependency = job.Schedule();
    }
}