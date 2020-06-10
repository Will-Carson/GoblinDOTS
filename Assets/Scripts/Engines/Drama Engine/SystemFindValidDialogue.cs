using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemFindValidDialogue : SystemBase
{
    
    [BurstCompile]
    struct SystemFindValidDialogueJob : IJob
    {
        public void Execute()
        {
            
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new SystemFindValidDialogueJob();
        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}