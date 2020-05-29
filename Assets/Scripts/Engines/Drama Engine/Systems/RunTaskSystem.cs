using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
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
            // Process task request events
            for (int i = 0; i < eventsTaskRequest.Length; i++)
            {
                runningTasks.Add(eventsTaskRequest[i]);
            }

            // Process task complete events
            NativeList<int> completeTaskIds = new NativeList<int>();

            for (int i = 0; i < eventsTaskComplete.Length; i++)
            {
                for (int j = 0; j < runningTasks.Length; j++)
                {
                    if (eventsTaskComplete[i].characterId == runningTasks[j].characterId)
                    {
                        completeTaskIds.Add(i);
                    }
                }
            }

            for (int i = completeTaskIds.Length; i != 0; i--)
            {
                runningTasks.RemoveAtSwapBack(completeTaskIds[i]);
            }
            
            // Broadcast to clients the next task event
            for (int i = 0; i < runningTasks.Length; i++)
            {
                // TODO
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