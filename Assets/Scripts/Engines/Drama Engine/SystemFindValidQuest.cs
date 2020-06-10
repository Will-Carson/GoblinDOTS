using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;
using System;

[ServerWorld]
public class SystemFindValidQuest : SystemBase
{
    [AutoAssign] SystemLocationManager LMS;
    [AutoAssign] SystemCheckQuest CQS;
    [AutoAssign] SystemWorldStateEvaluation WSES;

    public NativeList<EventQuestRequest> EventsQuestRequest = new NativeList<EventQuestRequest>(G.maxPlayerPopulation, Allocator.Persistent);
    public NativeArray<QuestRequirement> QRL = new NativeArray<QuestRequirement>(G.numberOfQuests, Allocator.Persistent);
    public NativeArray<int> QuestId = new NativeArray<int>(1, Allocator.Persistent);

    protected override void OnCreate()
    {
        QuestId[0] = 0;

        // Example of adding a quest.
        QRL[0] = new QuestRequirement();
    }

    [BurstCompile]
    struct SystemFindValidQuestJob : IJob
    {
        [ReadOnly] public NativeHashMap<int, DataLocation> cl;
        [ReadOnly] public NativeArray<DataWorldState> ws;
        public NativeList<EventQuestRequest> eventsQuestRequest;
        [ReadOnly] public NativeArray<QuestRequirement> qrl;
        public NativeArray<int> questId;
        public NativeHashMap<int, DataValidQuest> currentQuests;
        public NativeHashMap<int, EventQuestRequest> currentQuestRequests;

        

        public void Execute()
        {
            var validQuest = new DataValidQuest();
            var validQuests = new NativeList<DataValidQuest>(G.numberOfQuests, Allocator.Temp);

            

            for (int i = 0; i < eventsQuestRequest.Length; i++)
            {
                var e = eventsQuestRequest[i];
                for (int j = 0; j < qrl.Length; j++)
                {
                    if (Requirements(qrl[j], out validQuest, ws[cl[e.giverId].stageId]))
                    {
                        validQuests.Add(validQuest);
                    }
                }

                // Just adding the first quest to the current quests list. TODO do something more interesting.
                int questId = GetNextQuestId();
                currentQuests.Add(questId, validQuests[0]);
                currentQuestRequests.Add(questId, e);
            }
            eventsQuestRequest.Clear();

            // Dispose
            validQuests.Dispose();
        }

        private int GetNextQuestId()
        {
            return questId[0]++;
        }

        // I may or may not need vqs and vqo to be out variables. Since NativeLists are just references to spots in memory, this should work.
        bool Requirements(QuestRequirement qr, out DataValidQuest vq, DataWorldState ws)
        {
            vq = new DataValidQuest();
            // TODO Oh god oh fuck

            return true;
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new SystemFindValidQuestJob()
        {
            currentQuests = CQS.CurrentQuests,
            currentQuestRequests = CQS.CurrentQuestRequests,
            eventsQuestRequest = EventsQuestRequest,
            cl = LMS.CharacterLocations,
            ws = WSES.DatasWorldState,
            qrl = QRL,
            questId = QuestId
        };

        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventsQuestRequest.Dispose();
        QRL.Dispose();
    }
}