using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

public class SystemTemplate : SystemBase
{
    [BurstCompile]
    struct SystemRunDialogueJob : IJob
    {

        public void Execute()
        {

        }
    }

    protected override void OnUpdate()
    {

    }

    protected override void OnDestroy()
    {
        
    }
}