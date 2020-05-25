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
    // List of structs to handle new faction member creation
    // List of structs to handle new faction creation
    // List of structs to handle faction reparenting
    
    // Struct / event types to handle each of the above cases

    public NativeHashMap<int, FactionMemberStruct> FactionMembers = new NativeHashMap<int, FactionMemberStruct>();
    public NativeHashMap<int, Faction> Factions = new NativeHashMap<int, Faction>();
    
    [BurstCompile]
    struct ManageFactionAndFactionMembersJob : IJob
    {
        // Parallels to the above lists       
        
        public void Execute()
        {
            // Create new factions
            // Create new faction members
            // reparent factions
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new ManageFactionAndFactionMembersJob();
        
        
        job.Schedule();
    }
}