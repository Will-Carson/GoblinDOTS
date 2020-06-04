﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemCheckQuest : SystemBase
{

    public NativeHashMap<int, DataValidQuest> CurrentQuests = new NativeHashMap<int, DataValidQuest>(G.maxCurrentQuests, Allocator.Persistent);
    public NativeHashMap<int, EventQuestRequest> CurrentQuestRequests = new NativeHashMap<int, EventQuestRequest>(G.maxCurrentQuests, Allocator.Persistent);
    public NativeMultiHashMap<int, int> QuestSubjects = new NativeMultiHashMap<int, int>(G.maxQuestSubjects, Allocator.Persistent);
    public NativeMultiHashMap<int, int> QuestObjects = new NativeMultiHashMap<int, int>(G.maxQuestObjects, Allocator.Persistent);

    [BurstCompile]
    struct SystemCheckQuestJob : IJob
    {
        // Add fields here that your job needs to do its work.
        // For example,
        //    public float deltaTime;
        
        
        
        public void Execute()
        {
            // Implement the work to perform for each entity here.
            // You should only access data that is local or that is a
            // field on this job. Note that the 'rotation' parameter is
            // marked as [ReadOnly], which means it cannot be modified,
            // but allows this job to run in parallel with other jobs
            // that want to read Rotation component data.
            // For example,
            //     translation.Value += mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;
            
            
        }
    }
    
    protected override void OnUpdate()
    {
        
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CurrentQuests.Dispose();
        QuestSubjects.Dispose();
        QuestObjects.Dispose();
    }

    public JobHandle ScheduleEvent()
    {
        var job = new SystemCheckQuestJob();
        return job.Schedule();
    }
}