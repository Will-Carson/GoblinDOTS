using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemRunDialogue : SystemBase
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

    public JobHandle ScheduleEvent()
    {
        var job = new SystemRunDialogueJob();
        return job.Schedule();
    }
}