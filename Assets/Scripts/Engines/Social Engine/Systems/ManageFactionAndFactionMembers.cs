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
    public NativeList<FactionCreationEvent> FactionCreationEvents = new NativeList<FactionCreationEvent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<FactionMemberCreationEvent> FactionMemberCreationEvents = new NativeList<FactionMemberCreationEvent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<FactionAddParentEvent> FactionAddParentEvents = new NativeList<FactionAddParentEvent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<FactionRemoveParentEvent> FactionRemoveParentEvents = new NativeList<FactionRemoveParentEvent>(G.rareFactionEvents, Allocator.Persistent);
    public NativeList<ChangeFactionPowerEvent> ChangeFactionPowerEvents = new NativeList<ChangeFactionPowerEvent>(G.rareFactionEvents, Allocator.Persistent);

    public NativeHashMap<int, Faction> Factions = new NativeHashMap<int, Faction>(G.maxFactions, Allocator.Persistent);
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
        public NativeList<FactionCreationEvent> factionCreationEvents;
        public NativeList<FactionMemberCreationEvent> factionMemberCreationEvents;
        public NativeList<FactionAddParentEvent> factionAddParentEvents;
        public NativeList<FactionRemoveParentEvent> factionRemoveParentEvents;
        public NativeList<ChangeFactionPowerEvent> changeFactionPowerEvents;

        public NativeHashMap<int, Faction> factions;
        public NativeHashMap<int, FactionMember> factionMembers;

        public NativeArray<int> nextFactionId;
        public NativeArray<int> nextFactionMemberId;

        public void Execute()
        {
            // Create new factions
            for (int i = 0; i < factionCreationEvents.Capacity; i++)
            {
                var e = factionCreationEvents[i];
                var newFaction = new Faction()
                {
                    parentIds = e.parentIds,
                    personalityTraits = e.personalityTraits
                };

                factions.Add(GetNextFactionID(), newFaction);
            }

            // Create new faction members
            for (int i = 0; i < factionMemberCreationEvents.Capacity; i++)
            {
                var e = factionMemberCreationEvents[i];
                var newFactionMember = new FactionMember()
                {
                    factionId = e.factionId,
                    power = e.power
                };

                factionMembers.Add(GetNextFactionMemberID(), newFactionMember);
            }

            // Add parent to factions
            for (int i = 0; i < factionAddParentEvents.Capacity; i++)
            {
                var e = factionAddParentEvents[i];
                var f = factions[e.subjectFactionId];
                var newParentIds = new int[f.parentIds.Length + 1];

                for (int j = 0; j < newParentIds.Length; j++)
                {
                    if (j < f.parentIds.Length)
                    {
                        newParentIds[j] = f.parentIds[j];
                    }
                    else
                    {
                        newParentIds[j] = e.newFactionParentId;
                    }
                }

                f.parentIds = newParentIds;
            }

            // Remove parent from faction
            for (int i = 0; i < factionRemoveParentEvents.Capacity; i++)
            {
                var e = factionRemoveParentEvents[i];
                var f = factions[e.subjectFactionId];
                var newParentIds = new int[f.parentIds.Length + 1];

                for (int j = 0; j < newParentIds.Length; j++)
                {
                    if (j < f.parentIds.Length)
                    {
                        newParentIds[j] = f.parentIds[j];
                    }
                    else
                    {
                        newParentIds[j] = e.removeFactionParentId;
                    }
                }

                f.parentIds = newParentIds;
            }

            // Change faction member power
            for (int i = 0; i < changeFactionPowerEvents.Capacity; i++)
            {
                var e = changeFactionPowerEvents[i];
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
    
    protected override void OnUpdate()
    {
        var job = new ManageFactionAndFactionMembersJob()
        {
            factionCreationEvents = FactionCreationEvents,
            factionMemberCreationEvents = FactionMemberCreationEvents,
            factionAddParentEvents = FactionAddParentEvents,
            factionRemoveParentEvents = FactionRemoveParentEvents,
            changeFactionPowerEvents = ChangeFactionPowerEvents,
            nextFactionId = NextFactionId,
            nextFactionMemberId = NextFactionMemberId
        };
        
        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        FactionCreationEvents.Dispose();
        FactionMemberCreationEvents.Dispose();
        FactionAddParentEvents.Dispose();
        FactionRemoveParentEvents.Dispose();
        ChangeFactionPowerEvents.Dispose();
        NextFactionId.Dispose();
        NextFactionMemberId.Dispose();
    }
}