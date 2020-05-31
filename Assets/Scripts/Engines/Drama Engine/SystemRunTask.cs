using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

// TODO just using max population for all Native* type allocation. Create more specific values.
[ServerWorld]
public class SystemRunTask : SystemBase
{
    // Events:
    public NativeList<EventTaskRequest> EventsTaskRequest = new NativeList<EventTaskRequest>(GlobalVariables.maxNPCPopulation, Allocator.Persistent);
    public NativeList<EventTaskContinue> EventsTaskContinue = new NativeList<EventTaskContinue>(GlobalVariables.maxNPCPopulation, Allocator.Persistent);
    public NativeList<EventTaskComplete> EventsTaskComplete = new NativeList<EventTaskComplete>(GlobalVariables.maxNPCPopulation, Allocator.Persistent);

    public NativeList<EventTaskRequest> RunningTasks = new NativeList<EventTaskRequest>(GlobalVariables.maxNPCPopulation, Allocator.Persistent);

    [BurstCompile]
    struct SystemRunTaskJob : IJob
    {
        public NativeList<EventTaskRequest> eventsTaskRequest;
        public NativeList<EventTaskContinue> eventsTaskContinue;
        public NativeList<EventTaskComplete> eventsTaskComplete;

        public NativeList<EventTaskRequest> runningTasks;

        public void Execute()
        {
            // Process task request events
            for (int i = 0; i < eventsTaskRequest.Length; i++)
            {
                runningTasks.Add(eventsTaskRequest[i]);
            }

            // Process task continue events
            for (int i = 0; i < eventsTaskContinue.Length; i++)
            {
                // TODO finish this
            }

            // Process task complete events
            NativeList<int> completeTaskIds = new NativeList<int>(GlobalVariables.maxNPCPopulation, Allocator.Temp);

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

            // Dispose of temporary Native*
            completeTaskIds.Dispose();
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new SystemRunTaskJob()
        {
            eventsTaskRequest = EventsTaskRequest,
            eventsTaskContinue = EventsTaskContinue,
            eventsTaskComplete = EventsTaskComplete,
            runningTasks = RunningTasks
        };

        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventsTaskRequest.Dispose();
        EventsTaskContinue.Dispose();
        EventsTaskRequest.Dispose();
        RunningTasks.Dispose();
    }
}