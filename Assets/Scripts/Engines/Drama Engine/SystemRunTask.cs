using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemRunTask : SystemBase
{
    // Track running tasks
    // Progress running tasks
    // Send task progress messages to clients

    protected override void OnUpdate()
    {

    }

    protected override void OnDestroy()
    {
        
    }
}