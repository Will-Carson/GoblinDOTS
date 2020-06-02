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
    public NativeList<EventFactionMemberCreate> EventsFactionMemberCreate = new NativeList<EventFactionMemberCreate>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<EventFactionAddParent> EventsFactionAddParent = new NativeList<EventFactionAddParent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<EventFactionRemoveParent> EventsFactionRemoveParent = new NativeList<EventFactionRemoveParent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<EventChangeFactionPower> EventsChangeFactionPower = new NativeList<EventChangeFactionPower>(G.rareFactionEvents, Allocator.Persistent);

    public NativeHashMap<int, DataFaction> Factions = new NativeHashMap<int, DataFaction>(G.maxFactions, Allocator.Persistent);
    public NativeMultiHashMap<int, int> FactionParents = new NativeMultiHashMap<int, int>(G.maxFactions * G.maxFactionParents, Allocator.Persistent);
    public NativeMultiHashMap<int, int> FactionRelationshipIds = new NativeMultiHashMap<int, int>(G.maxFactions * G.maxFactionParents, Allocator.Persistent);

    public NativeHashMap<int, FactionMember> FactionMembers = new NativeHashMap<int, FactionMember>(G.maxFactions, Allocator.Persistent);

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
        public NativeList<EventFactionMemberCreate> eventsFactionMemberCreate;
        public NativeList<EventFactionAddParent> eventsFactionAddParent;
        public NativeList<EventFactionRemoveParent> eventsFactionRemoveParent;
        public NativeList<EventChangeFactionPower> eventsChangeFactionPower;

        public NativeHashMap<int, DataFaction> factions;
        public NativeMultiHashMap<int, int> factionParents;
        public NativeMultiHashMap<int, int> factionRelationshipIds;

        public NativeHashMap<int, FactionMember> factionMembers;

        public NativeArray<int> nextFactionId;
        public NativeArray<int> nextFactionMemberId;

        // TODO seperate these into different jobs
        public void Execute()
        {
            // Create new factions
            if (eventsFactionCreate.Length != 0)
            {
                for (int i = 0; i < eventsFactionCreate.Capacity; i++)
                {
                    var id = GetNextFactionID();
                    var e = eventsFactionCreate[i];
                    var newFaction = new DataFaction();

                    var parents = eventsFactionCreateParents.GetValuesForKey(i);
                    do
                    {
                        factionParents.Add(id, parents.Current);
                    } while (parents.MoveNext());

                    factions.Add(id, newFaction);

                    factions[id].personality.SetValues(e.values.GetValues(Allocator.TempJob));
                }
            }

            // Create new faction members
            if (eventsFactionMemberCreate.Length != 0)
            {
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
            }

            // Add parent to factions
            if (eventsFactionAddParent.Length != 0)
            {
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
            }

            // Remove parent from faction
            if (eventsFactionRemoveParent.Length != 0)
            {
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

                    // Dispose
                    f.Dispose();
                }
            }
            
            // Change faction member power
            if (eventsChangeFactionPower.Length != 0)
            {
                for (int i = 0; i < eventsChangeFactionPower.Capacity; i++)
                {
                    var e = eventsChangeFactionPower[i];
                    var newFactionMember = factionMembers[i];
                    newFactionMember.power = e.newPower;
                    factionMembers[i] = newFactionMember;
                }
            }
        }

        // TODO finish these functions lol
        private int GetNextFactionID()
        {
            return nextFactionId[0]++;
        }

        private int GetNextFactionMemberID()
        {
            return nextFactionMemberId[0]++;
        }

        // Dispose
    }
    

    public NativeArray<int> nextFactionMemberId;
    protected override void OnUpdate()
    {
        var job = new ManageFactionAndFactionMembersJob()
        {
            eventsFactionCreate = EventsFactionCreate,
            eventsFactionCreateParents = EventsFactionCreateParents,
            eventsFactionMemberCreate = EventsFactionMemberCreate,
            eventsFactionAddParent = EventsFactionAddParent,
            eventsFactionRemoveParent = EventsFactionRemoveParent,
            eventsChangeFactionPower = EventsChangeFactionPower,
            factions = Factions,
            factionParents = FactionParents,
            factionRelationshipIds = FactionRelationshipIds,
            factionMembers = FactionMembers,
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
        EventsFactionMemberCreate.Dispose();
        EventsFactionAddParent.Dispose();
        EventsFactionRemoveParent.Dispose();
        EventsChangeFactionPower.Dispose();
        Factions.Dispose();
        FactionParents.Dispose();
        FactionRelationshipIds.Dispose();
        FactionMembers.Dispose();
        NextFactionId.Dispose();
        NextFactionMemberId.Dispose();
    }
}