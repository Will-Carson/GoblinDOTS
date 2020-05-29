using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class RunTaskSystem : SystemBase
{
    // Events:
    public NativeList<EventTaskRequest> EventsTaskRequest = new NativeList<EventTaskRequest>();
    public NativeList<EventTaskContinue> EventsTaskContinue = new NativeList<EventTaskContinue>();
    public NativeList<EventTaskComplete> EventsTaskComplete = new NativeList<EventTaskComplete>();

    public NativeList<EventTaskRequest> RunningTasks = new NativeList<EventTaskRequest>();

    [BurstCompile]
    struct RunTaskSystemJob : IJob
    {
        public NativeList<EventTaskRequest> eventsTaskRequest;
        public NativeList<EventTaskComplete> eventsTaskComplete;

        public NativeList<EventTaskRequest> runningTasks;

        public void Execute()
        {
            for (int i = 0; i < eventsTaskRequest.Length; i++)
            {
                // Process task request events
            }

            for (int i = 0; i < eventsTaskComplete.Length; i++)
            {
                // Process task complete events
            }

            for (int i = 0; i < runningTasks.Length; i++)
            {
                // Broadcast to clients the next t
            }
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new RunTaskSystemJob();
        
        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        //     job.deltaTime = UnityEngine.Time.deltaTime;
        
        
        
        // Now that the job is set up, schedule it to be run. 
        job.Schedule();
    }
}