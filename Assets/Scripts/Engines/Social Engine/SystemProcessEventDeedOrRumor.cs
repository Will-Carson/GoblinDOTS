using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemProcessDeedOrRumorEvent : SystemBase
{
    [AutoAssign] protected SystemManageFactionAndFactionMembers FactionSystem;

    public NativeArray<DataDeed> DeedLibrary = new NativeArray<DataDeed>(G.numberOfDeeds, Allocator.Persistent);

    public NativeList<EventRumorOrDeed> EventsRumorOrDeed = new NativeList<EventRumorOrDeed>(G.maxNPCPopulation, Allocator.Persistent);
    public NativeMultiHashMap<int, int> EventsRumorOrDeedWitnesses = new NativeMultiHashMap<int, int>(G.maxTotalListeners, Allocator.Persistent);

    public NativeHashMap<int, DataRelationship> Relationships = new NativeHashMap<int, DataRelationship>(G.maxRelationships, Allocator.Persistent);
    public NativeMultiHashMap<int, int> RelationshipIds = new NativeMultiHashMap<int, int>(G.maxRelationships, Allocator.Persistent);
    public NativeHashMap<int, DataMemory> Memories = new NativeHashMap<int, DataMemory>(G.maxMemories, Allocator.Persistent);
    public NativeMultiHashMap<int, int> MemoryIds = new NativeMultiHashMap<int, int>(G.maxMemories, Allocator.Persistent);

    public NativeArray<int> NextRelationshipId = new NativeArray<int>(1, Allocator.Persistent);
    public NativeArray<int> NextMemoryId = new NativeArray<int>(1, Allocator.Persistent);

    protected override void OnCreate()
    {
        // TODO in the future these values should be pulled from a database instead of being set to 0
        // This will facilitate server restarts.
        NextRelationshipId[0] = 0;
        NextMemoryId[0] = 0;

        // Add deed example
        DeedLibrary[0] = new DataDeed(){ values = new DataValues() { /* Define deed here */ } };
    }
    
    [BurstCompile]
    struct ProcessDeedOrRumorEventJob : IJob
    {
        public NativeArray<DataDeed> dataDeedLibrary;
        public NativeHashMap<int, FactionMember> factionMembers;
        public NativeHashMap<int, DataFaction> factions;
        public NativeList<EventRumorOrDeed> eventsRumorOrDeed;
        public NativeMultiHashMap<int, int> eventsRumorOrDeedWitnesses;
        public NativeMultiHashMap<int, int> relationshipIds;
        public NativeHashMap<int, DataRelationship> relationships;
        public NativeMultiHashMap<int, int> memoryIds;
        public NativeHashMap<int, DataMemory> memories;
        public NativeArray<int> nextRelationshipId;
        public NativeArray<int> nextMemoryId;

        public void Execute()
        {
            for (int i = 0; i < eventsRumorOrDeed.Length; i++)
            {
                // Process rumor events into new memories
                if (eventsRumorOrDeed[i].needsEvaluation)
                {
                    var witnesses = eventsRumorOrDeedWitnesses.GetValuesForKey(i);
                    foreach (var witness in witnesses) ProcessEvent(witness, eventsRumorOrDeed[i]);
                }
            }
        }

        private void SortEvent(int witness, EventRumorOrDeed e)
        {
            var r = new EventRumorOrDeed();
            if (e.isRumor)
            {
                r = new EventRumorOrDeed()
                {
                    needsEvaluation = false,
                    rumorSpreader = e.rumorSpreader,
                    deedDoer = e.deedDoer,
                    type = e.type,
                    deedTarget = e.deedTarget,
                    reliability = e.reliability
                };
            }
            else
            {
                r = new EventRumorOrDeed()
                {
                    needsEvaluation = false,
                    rumorSpreader = witness,
                    deedDoer = e.deedDoer,
                    type = e.type,
                    deedTarget = e.deedTarget,
                    reliability = 1
                };
            }

            ProcessEvent(witness, r);
        }

        private void ProcessEvent(int witness, EventRumorOrDeed re)
        {
            FactionMember fm = factionMembers[witness];
            DataMemory m = new DataMemory();
            if (witness != re.rumorSpreader)
            {
                m.reliability = GetReliability(witness, re);
            }

            m.rumorSpreader = re.rumorSpreader;
            m.deedDoer = re.deedDoer;
            m.type = re.type;
            m.deedTarget = re.deedTarget;
            m.timesCommitted = 1;

            #region affinity calculation

            // Process the memory into their relationship
            float affinityDelta = 0;

            // Calculate affinity change from witness relationship to target
            affinityDelta += GetAffinity(witness, m.deedTarget) * m.impact * m.reliability;
            affinityDelta += abs(affinityDelta) * GetTraitAlignment(witness, m.type) * G.traitAlignmentImportance; // TODO magic number "trait alignment importance"
            affinityDelta += GetAffinity(witness, m.deedDoer) * fm.mood.arousal * G.arrousalImportance; // TODO magic number "arousal importance

            var faction = factions[fm.factionId];
            var factionRelationshipIds = relationshipIds.GetValuesForKey(fm.factionId);
            bool establishedRelationship = false;

            foreach (var rid in factionRelationshipIds)
            {
                var relationship = relationships[fm.factionId];
                if (relationship.targetId == m.deedDoer)
                {
                    // Update relationship
                    relationship.relationshipValues.affinity += affinityDelta * pow(.8f, m.timesCommitted) * affinityDelta;
                    m.impact = affinityDelta * pow(.8f, m.timesCommitted) * affinityDelta;
                    establishedRelationship = true;
                }
            }

            if (!establishedRelationship)
            {
                DataRelationship relationship = new DataRelationship()
                {
                    targetId = m.deedDoer,
                    relationshipValues = new RelationshipValues()
                    {
                        affinity = affinityDelta
                    }
                };

                var relationshipId = GetNextRelationshipId();
                relationshipIds.Add(fm.factionId, relationshipId);
                relationships.Add(relationshipId, relationship);
            }

            #endregion

            #region add memory
            
            int memoryId = 0;

            // Add memory to memories
            // Check if the memory is a duplicate
            // If this character has too many memories, replace the lowest impact memory.
            // Only if the lowest impact memory is lower impact then the new memory.
            var mids = memoryIds.GetValuesForKey(witness);
            var midNum = memoryIds.CountValuesForKey(witness);
            foreach (var mid in mids)
            {
                if (midNum >= G.memoriesPerCharacter)
                {
                    float lowestImpact = 1;
                    int lowestImpactMemoryId = 0;
                    if (memories[mid].impact < lowestImpact)
                    {
                        lowestImpact = memories[mid].impact;
                        lowestImpactMemoryId = mid;
                    }

                    if (m.impact < memories[lowestImpactMemoryId].impact)
                    {
                        memoryId = lowestImpactMemoryId;
                    }
                }
                else
                {
                    var c = memories[mid];
                    if (c.Equals(m))
                    {
                        memoryId = mid;
                        m.timesCommitted = c.timesCommitted + 1;
                    }
                }
            }

            // Otherwise, just give them the next memory ID.
            if (memoryId == 0) { memoryId = GetNextMemoryId(); }

            // Add the memory to the faction members memories, as well as the memories hashset
            memories.Add(memoryId, m);
            memoryIds.Add(witness, memoryId);

            #endregion

            #region adjust mood

            // Process memory into faction member mood
            float pleasureDelta = GetAffinity(witness, m.deedDoer) + (GetAffinity(witness, m.deedDoer) * GetTraitAlignment(witness, m.type));
            float arousalDelta = abs(GetAffinity(witness, m.deedDoer)) * .2f; // TODO magic number "arousal importance"
            float dominanceDelta = sign(m.impact) * sign(GetAffinity(witness, m.deedTarget)) * abs(GetDeed(m.type).Aggression) * abs(affinityDelta);
            dominanceDelta += abs(dominanceDelta) * GetPowerCurve(fm.power - factionMembers[m.deedDoer].power);

            factionMembers[witness] = new FactionMember()
            {
                factionId = factionMembers[witness].factionId,
                power = factionMembers[witness].power,
                mood = new DataMood()
                {
                    pleasure = factionMembers[witness].mood.pleasure + pleasureDelta,
                    arousal = factionMembers[witness].mood.pleasure + arousalDelta,
                    dominance = factionMembers[witness].mood.pleasure + dominanceDelta
                }
            };

            #endregion
        }

        private float GetPowerCurve(float xpos)
        {
            var a = 10;
            var result = pow(a, xpos) - 1;
            result = result / (a - 1);
            return result;
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

        private float GetReliability(int witness, EventRumorOrDeed re)
        {
            return GetAffinity(witness, re.rumorSpreader) * re.reliability;
        }

        private float GetAffinity(int master, int target)
        {
            var faction = factions[factionMembers[master].factionId];
            var rids = relationshipIds.GetValuesForKey(factionMembers[master].factionId);

            foreach (var rid in rids)
            {
                if (rid == factionMembers[target].factionId)
                {
                    return relationships[rid].relationshipValues.affinity;
                }
            }

            return 0;
            
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

            // Todo necessary?
            if (deedTraits.Length < G.valuesTraits)
            {
                // Deed / faction trait # misalignment
                return 0;
            }

            // Get the difference between the character and the deeds traits. Will produce a number between 0 and 1.
            // 0 being the least possible alignment and 1 being the most possible alignment.
            var traits = faction.personality.GetValues(Allocator.Temp);
            for (int i = 0; i < traits.Length; i++)
            {
                traitAlignment -= abs(traits[i] - deedTraits[i]);
            }

            traitAlignment = traitAlignment / deedTraits.Length / 2;

            // Dispose
            deedTraits.Dispose();
            traits.Dispose();

            return traitAlignment;
        }

        private NativeArray<float> GetDeedTraits(int deed)
        {
            var values = GetDeed(deed).values.GetValues(Allocator.TempJob);
            NativeList<float> result = new NativeList<float>(G.valuesPerDeed, Allocator.TempJob);

            foreach (var value in values)
            {
                result.AddNoResize(value);
            }

            return result;
        }

        public DataDeed GetDeed(int deedId)
        {
            return dataDeedLibrary[deedId];
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new ProcessDeedOrRumorEventJob()
        {
            dataDeedLibrary = DeedLibrary,
            factionMembers = FactionSystem.FactionMembers,
            factions = FactionSystem.Factions,
            eventsRumorOrDeed = EventsRumorOrDeed,
            eventsRumorOrDeedWitnesses = EventsRumorOrDeedWitnesses,
            relationships = Relationships,
            relationshipIds = RelationshipIds,
            memories = Memories,
            memoryIds = MemoryIds,
            nextMemoryId = NextMemoryId,
            nextRelationshipId = NextRelationshipId
        };

        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DeedLibrary.Dispose();
        EventsRumorOrDeed.Dispose();
        EventsRumorOrDeedWitnesses.Dispose();
        Relationships.Dispose();
        RelationshipIds.Dispose();
        Memories.Dispose();
        MemoryIds.Dispose();
        NextMemoryId.Dispose();
        NextRelationshipId.Dispose();
    }
}