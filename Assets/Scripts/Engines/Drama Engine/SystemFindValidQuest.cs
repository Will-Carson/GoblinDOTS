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
public class SystemFindValidQuest : SystemBase
{
    [AutoAssign] SystemLocationManager LMS;
    
    public NativeArray<QuestRequirement> QRL = new NativeArray<QuestRequirement>(G.numberOfQuests, Allocator.Persistent);

    protected override void OnCreate()
    {
        // Example of adding a quest.
        QRL[0] = new QuestRequirement();
    }
    
    protected override void OnUpdate()
    {
        
    }

    protected override void OnDestroy()
    {
        QRL.Dispose();
    }
}