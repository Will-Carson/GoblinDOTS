using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

// TODO using a lot of GlobalVariables.numberOfStages, Allocator.Persistent. Should give these their own vars
// TODO PEL has no place here. This system isn't actually running the plays, so the PEL doesn't matter.
[ServerWorld]
public class SystemRunPlay<PE> : SystemBase where PE : unmanaged, IPlayExecution
{
    public NativeArray<PE> PEL = new NativeArray<PE>(G.numberOfPlays, Allocator.Persistent);

    // List of play related events
    public NativeList<EventPlayRequest> EventsPlayRequest = new NativeList<EventPlayRequest>(G.numberOfStages, Allocator.Persistent);
    public NativeList<EventPlayComplete> EventsPlayFinished = new NativeList<EventPlayComplete>(G.numberOfStages, Allocator.Persistent);
    public NativeList<EventPlayContinueRequest> EventsPlayContinueRequest = new NativeList<EventPlayContinueRequest>(G.numberOfStages, Allocator.Persistent);
    public NativeList<EventPlayRequest> ActivePlays = new NativeList<EventPlayRequest>(G.numberOfStages, Allocator.Persistent);

    protected override void OnCreate()
    {
        dynamic p;
        PEL[0] = p = new PlayETest();
    }

    [BurstCompile]
    struct RunPlaySystemJob : IJob
    {
        public NativeArray<PE> pel;

        public NativeList<EventPlayRequest> eventPlayRequests;
        public NativeList<EventPlayComplete> eventPlaysFinished;
        public NativeList<EventPlayContinueRequest> eventPlayContinueRequests;
        public NativeList<EventPlayRequest> activePlays;

        public void Execute()
        {
            // EventPlayRequest
            for (int i = 0; i < eventPlayRequests.Length; i++)
            {
                if (eventPlayRequests[i].stageId != 0)
                {
                    activePlays.Add(eventPlayRequests[i]);
                    // TODO send current play to clients.
                }
            }
            eventPlayRequests.Clear();
            
            // EventPlayContinueRequests
            for (int i = 0; i < eventPlayContinueRequests.Length; i++)
            {
                BroadcastContinue(eventPlayContinueRequests[i].stageId);
            }
            eventPlayContinueRequests.Clear();

            // EventPlaysFinished
            NativeList<int> removed = new NativeList<int>(G.numberOfStages, Allocator.Temp);
            for (int i = 0; i < eventPlaysFinished.Length; i++)
            {
                for (int j = 0; j < activePlays.Length; j++)
                {
                    if (eventPlaysFinished[i].stageId == activePlays[j].stageId)
                    {
                        removed.Add(j);
                    }
                }
            }
            eventPlaysFinished.Clear();

            // Removes elements in reverse order so we don't run over the lists size
            for (int i = removed.Length - 1; i == 0; i--) // TODO watch this closely.. could cause issues.
            {
                activePlays.RemoveAtSwapBack(removed[i]);
            }

            // Dispose
            removed.Dispose();
        }

        private void BroadcastContinue(int stageId)
        {
            // TODO make message and broadcast system for this... Also need to define the dialogue system more generally.
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new RunPlaySystemJob()
        {
            pel = PEL,
            eventPlayRequests = EventsPlayRequest,
            eventPlaysFinished = EventsPlayFinished,
            eventPlayContinueRequests = EventsPlayContinueRequest,
            activePlays = ActivePlays
        };
        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PEL.Dispose();
        EventsPlayRequest.Dispose();
        EventsPlayFinished.Dispose();
        EventsPlayContinueRequest.Dispose();
        ActivePlays.Dispose();
    }
}