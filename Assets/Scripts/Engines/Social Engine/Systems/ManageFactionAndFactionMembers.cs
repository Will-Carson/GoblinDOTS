using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class ManageFactionAndFactionMembers : SystemBase
{
    public NativeList<EventFactionCreate> EventsFactionCreate = new NativeList<EventFactionCreate>(G.rareFactionEvents, Allocator.Persistent);
    public NativeMultiHashMap<int, int> EventsFactionCreateParents = new NativeMultiHashMap<int, int>(G.rareFactionEvents * G.maxFactionParents, Allocator.Persistent);
    public NativeMultiHashMap<int, float> EventsFactionCreatePersonalityTraits = new NativeMultiHashMap<int, float>(G.rareFactionEvents * G.personalityTraits, Allocator.AudioKernel);
    public NativeList<EventFactionMemberCreate> EventsFactionMemberCreate = new NativeList<EventFactionMemberCreate>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<EventFactionAddParent> EventsFactionAddParent = new NativeList<EventFactionAddParent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<EventFactionRemoveParent> EventsFactionRemoveParent = new NativeList<EventFactionRemoveParent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<EventChangeFactionPower> EventsChangeFactionPower = new NativeList<EventChangeFactionPower>(G.rareFactionEvents, Allocator.Persistent);

    public NativeHashMap<int, Faction> Factions = new NativeHashMap<int, Faction>(G.maxFactions, Allocator.Persistent);
    public NativeMultiHashMap<int, int> FactionParents = new NativeMultiHashMap<int, int>(G.maxFactions * G.maxFactionParents, Allocator.Persistent);
    public NativeMultiHashMap<int, float> FactionPersonalityTraits = new NativeMultiHashMap<int, float>(G.maxFactions * G.personalityTraits, Allocator.AudioKernel);
    public NativeMultiHashMap<int, int> FactionRelationshipIds = new NativeMultiHashMap<int, int>(G.maxFactions * G.maxFactionParents, Allocator.Persistent);

    public NativeHashMap<int, FactionMember> FactionMembers = new NativeHashMap<int, FactionMember>(G.maxFactions, Allocator.Persistent);
    public NativeMultiHashMap<int, int> FactionMemberMemoryId = new NativeMultiHashMap<int, int>(G.maxMemories, Allocator.Persistent);
    public NativeMultiHashMap<int, float> FactionMemberMood = new NativeMultiHashMap<int, float>(G.totalMoodFloat, Allocator.Persistent);

    public NativeArray<int> NextFactionId = new NativeArray<int>(1, Allocator.Persistent);
    public NativeArray<int> NextFactionMemberId = new NativeArray<int>(1, Allocator.Persistent);

    protected override void OnCreate()
    {
        // TODO pull these from a database
        NextFactionId[0] = 0;
        NextFactionMemberId[0] = 0;
    }

    [BurstCompile]
    struct ManageFactionAndFactionMembersJob : IJob
    {
        public NativeList<EventFactionCreate> eventsFactionCreate;
        public NativeMultiHashMap<int, int> eventsFactionCreateParents;
        public NativeMultiHashMap<int, float> eventsFactionCreatePersonalityTraits;
        public NativeList<EventFactionMemberCreate> eventsFactionMemberCreate;
        public NativeList<EventFactionAddParent> eventsFactionAddParent;
        public NativeList<EventFactionRemoveParent> eventsFactionRemoveParent;
        public NativeList<EventChangeFactionPower> eventsChangeFactionPower;

        public NativeHashMap<int, Faction> factions;
        public NativeMultiHashMap<int, int> factionParents;
        public NativeMultiHashMap<int, float> factionPersonalityTraits;
        public NativeMultiHashMap<int, int> factionRelationshipIds;

        public NativeHashMap<int, FactionMember> factionMembers;
        public NativeMultiHashMap<int, int> factionMemberMemoryId;
        public NativeMultiHashMap<int, float> factionMemberMood;

        public NativeArray<int> nextFactionId;
        public NativeArray<int> nextFactionMemberId;

        // TODO seperate these into different jobs
        public void Execute()
        {
            // Create new factions
            for (int i = 0; i < eventsFactionCreate.Capacity; i++)
            {
                var id = GetNextFactionID();
                var e = eventsFactionCreate[i];
                var newFaction = new Faction();

                var parents = eventsFactionCreateParents.GetValuesForKey(i);
                do
                {
                    factionParents.Add(id, parents.Current);
                } while (parents.MoveNext());

                var personality = eventsFactionCreatePersonalityTraits.GetValuesForKey(i);
                do
                {
                    factionPersonalityTraits.Add(id, personality.Current);
                } while (personality.MoveNext());

                factions.Add(id, newFaction);
            }

            // Create new faction members
            for (int i = 0; i < eventsFactionMemberCreate.Capacity; i++)
            {
                var e = eventsFactionMemberCreate[i];
                var newFactionMember = new FactionMember()
                {
                    factionId = e.factionId,
                    power = e.power
                };

                factionMembers.Add(GetNextFactionMemberID(), newFactionMember);
            }

            // Add parent to factions
            for (int i = 0; i < eventsFactionAddParent.Capacity; i++)
            {
                var e = eventsFactionAddParent[i];
                var f = factionParents.GetValuesForKey(i);
                factionParents.Remove(e.subjectFactionId);
                var added = 0;

                do
                {
                    added++;
                    factionParents.Add(e.subjectFactionId, f.Current);
                } while (f.MoveNext() && added < G.maxFactionParents - 1);

                factionParents.Add(e.subjectFactionId, e.newFactionParentId);
            }

            // Remove parent from faction
            for (int i = 0; i < eventsFactionRemoveParent.Capacity; i++)
            {
                var e = eventsFactionRemoveParent[i];
                var f = factionParents.GetValuesForKey(i);
                factionParents.Remove(e.subjectFactionId);

                do
                {
                    if (f.Current != e.removeFactionParentId)
                    {
                        factionParents.Add(e.subjectFactionId, f.Current);
                    }
                } while (f.MoveNext());
            }

            // Change faction member power
            for (int i = 0; i < eventsChangeFactionPower.Capacity; i++)
            {
                var e = eventsChangeFactionPower[i];
                var newFactionMember = factionMembers[i];
                newFactionMember.power = e.newPower;
                factionMembers[i] = newFactionMember;
            }
        }

        // TODO finish these functions lol
        private int GetNextFactionID()
        {
            return 0;
        }

        private int GetNextFactionMemberID()
        {
            return 0;
        }
    }
    

    public NativeArray<int> nextFactionMemberId;
    protected override void OnUpdate()
    {
        var job = new ManageFactionAndFactionMembersJob()
        {
            eventsFactionCreate = EventsFactionCreate,
            eventsFactionCreateParents = EventsFactionCreateParents,
            eventsFactionCreatePersonalityTraits = EventsFactionCreatePersonalityTraits,
            eventsFactionMemberCreate = EventsFactionMemberCreate,
            eventsFactionAddParent = EventsFactionAddParent,
            eventsFactionRemoveParent = EventsFactionRemoveParent,
            eventsChangeFactionPower = EventsChangeFactionPower,
            factions = Factions,
            factionParents = FactionParents,
            factionPersonalityTraits = FactionPersonalityTraits,
            factionRelationshipIds = FactionRelationshipIds,
            factionMembers = FactionMembers,
            factionMemberMemoryId = FactionMemberMemoryId,
            factionMemberMood = FactionMemberMood,
            nextFactionId = NextFactionId,
            nextFactionMemberId = NextFactionMemberId
        };
        
        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventsFactionCreate.Dispose();
        EventsFactionCreateParents.Dispose();
        EventsFactionCreatePersonalityTraits.Dispose();
        EventsFactionMemberCreate.Dispose();
        EventsFactionAddParent.Dispose();
        EventsFactionRemoveParent.Dispose();
        EventsChangeFactionPower.Dispose();
        Factions.Dispose();
        FactionParents.Dispose();
        FactionPersonalityTraits.Dispose();
        FactionRelationshipIds.Dispose();
        FactionMembers.Dispose();
        FactionMemberMemoryId.Dispose();
        FactionMemberMood.Dispose();
        NextFactionId.Dispose();
        NextFactionMemberId.Dispose();
    }
}