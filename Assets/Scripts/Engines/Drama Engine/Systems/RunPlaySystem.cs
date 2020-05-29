﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class RunPlaySystem : SystemBase
{
    public NativeArray<PlayExecutionLibrary> PEL;

    // List of play related events
    public NativeList<EventPlayRequest> EventsPlayRequest;
    public NativeList<EventPlayComplete> EventsPlayFinished;
    public NativeList<EventPlayContinueRequest> EventsPlayContinueRequest;
    public NativeList<EventPlayRequest> ActivePlays;

    protected override void OnCreate()
    {
        PlayExecutionLibrary playExecutionLibrary = new PlayExecutionLibrary()
        {
            playExecutions = new IPlayExecution[]
            {
                // Define play execution library here
                // TODO write plays
            }
        };

        PEL[0] = playExecutionLibrary;
    }

    [BurstCompile]
    struct RunPlaySystemJob : IJob
    {
        public NativeArray<PlayExecutionLibrary> pel;

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

            // EventPlaysFinished
            NativeList<int> removed = new NativeList<int>();
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
            // Removes elements in reverse order so we don't run over the lists size
            for (int i = removed.Length - 1; i == 0; i--) // TODO watch this closely.. could cause issues.
            {
                eventPlaysFinished.RemoveAtSwapBack(removed[i]);
            }

            // EventPlayContinueRequests
            for (int i = 0; i < eventPlayContinueRequests.Length; i++)
            {
                BroadcastContinue(eventPlayContinueRequests[i].stageId);
            }
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
            eventPlayRequests = EventsPlayRequest,
            eventPlaysFinished = EventsPlayFinished,
            eventPlayContinueRequests = EventsPlayContinueRequest,
            activePlays = ActivePlays
        };
        job.Schedule();
    }
}