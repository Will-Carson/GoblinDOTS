﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemFindValidQuest<QR> : SystemBase where QR : struct, IQuestRequirements
{
    [AutoAssign] SystemLocationManager LMS;
    [AutoAssign] SystemCheckQuest CQS;
    [AutoAssign] SystemWorldStateEvaluation WSES;

    public NativeList<EventQuestRequest> EventsQuestRequest = new NativeList<EventQuestRequest>(GlobalVariables.maxPlayerPopulation, Allocator.Persistent);
    public NativeHashMap<EventQuestRequest, DataValidQuest> CurrentQuests = new NativeHashMap<EventQuestRequest, DataValidQuest>(GlobalVariables.maxCurrentQuests, Allocator.Persistent);

    private NativeArray<QR> QRL;
    
    protected override void OnCreate()
    {
        QRL = new NativeArray<QR>(GlobalVariables.numberOfQuests, Allocator.Persistent)
        {

        };
    }

    [BurstCompile]
    struct SystemFindValidQuestJob : IJob
    {
        public SystemLocationManager lms;
        public SystemCheckQuest cqs;
        public SystemWorldStateEvaluation wses;
        public NativeList<EventQuestRequest> eventsQuestRequest;
        private NativeArray<QR> qrl;
        public NativeHashMap<EventQuestRequest, DataValidQuest> currentQuests;

        public void Execute()
        {
            var validQuest = new DataValidQuest();
            var validQuests = new NativeList<DataValidQuest>(GlobalVariables.numberOfQuests, Allocator.Temp);
            for (int i = 0; i < eventsQuestRequest.Length; i++)
            {
                var e = eventsQuestRequest[i];
                for (int j = 0; j < qrl.Length; j++)
                {
                    qrl[j].Requirements(out validQuest, wses.DatasWorldState[lms.CharacterLocations[e.giverId].stageId]);
                    if (validQuest.questId != 0)
                    {
                        validQuests.Add(validQuest);
                    }
                }

                // Just adding the first quest to the current quests list. TODO do something more interesting.
                currentQuests.Add(e, validQuests[0]);
            }

            // Dispose
            validQuests.Dispose();
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new SystemFindValidQuestJob()
        {
            cqs = CQS,
            currentQuests = CurrentQuests,
            eventsQuestRequest = EventsQuestRequest,
            lms = LMS,
            wses = WSES
        };

        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventsQuestRequest.Dispose();
        CurrentQuests.Dispose();
        QRL.Dispose();
    }
}