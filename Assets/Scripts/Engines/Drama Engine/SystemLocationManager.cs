using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemLocationManager : SystemBase
{
    // Accessible data:
    // Character locations
    // Point/Stage/Site data (occupents, etc)
    // Move characters on request

    protected override void OnUpdate()
    {
        
    }
}