using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;
using System;

[ServerWorld]
public class SystemFindValidTask : SystemBase
{
    private NativeArray<TaskRequirement> TRL = new NativeArray<TaskRequirement>(G.numberOfTasks, Allocator.Persistent);

    protected override void OnCreate()
    {
        // Example of assigning a task
        TRL[0] = new TaskRequirement();
    }

    protected override void OnUpdate()
    {
        
    }

    protected override void OnDestroy()
    {
        TRL.Dispose();
    }
}