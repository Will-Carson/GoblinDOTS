using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

public class SystemFindValidDialogue : SystemBase, INonScheduler
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
        
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public JobHandle ScheduleEvent()
    {
        var job = new SystemFindValidDialogueJob(); 
        return job.Schedule();
    }
}