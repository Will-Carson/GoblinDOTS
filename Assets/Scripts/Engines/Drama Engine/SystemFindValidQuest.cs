using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemFindValidQuest<QR> : SystemBase 
    where QR : unmanaged, IQuestRequirements
{
    [AutoAssign] SystemLocationManager LMS;
    [AutoAssign] SystemCheckQuest CQS;
    [AutoAssign] SystemWorldStateEvaluation WSES;

    public NativeList<EventQuestRequest> EventsQuestRequest = new NativeList<EventQuestRequest>(G.maxPlayerPopulation, Allocator.Persistent);

    private NativeArray<QR> QRL = new NativeArray<QR>(G.numberOfQuests, Allocator.Persistent);

    protected override void OnCreate()
    {
        // Example of adding a quest.
        dynamic q = new QuestRTest();
        QRL[0] = q;
    }

    [BurstCompile]
    struct SystemFindValidQuestJob : IJob
    {
        public SystemLocationManager lms;
        public SystemWorldStateEvaluation wses;
        public NativeList<EventQuestRequest> eventsQuestRequest;
        private NativeArray<QR> qrl;
        public NativeHashMap<EventQuestRequest, DataValidQuest> currentQuests;
        public NativeMultiHashMap<EventQuestRequest, int> questSubjects;
        public NativeMultiHashMap<EventQuestRequest, int> questObjects;

        public void Execute()
        {
            var validQuest = new DataValidQuest();
            var validQuests = new NativeList<DataValidQuest>(G.numberOfQuests, Allocator.Temp);
            // valid quest subjects
            var vqs = new NativeList<int>(G.maxPerQuestSubjectsObjects, Allocator.Temp);
            // valid quest objects
            var vqo = new NativeList<int>(G.maxPerQuestSubjectsObjects, Allocator.Temp);

            for (int i = 0; i < eventsQuestRequest.Length; i++)
            {
                var e = eventsQuestRequest[i];
                for (int j = 0; j < qrl.Length; j++)
                {
                    qrl[j].Requirements(out validQuest, out vqs, out vqo, wses.DatasWorldState[lms.CharacterLocations[e.giverId].stageId]);
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
            currentQuests = CQS.CurrentQuests,
            questSubjects = CQS.QuestSubjects,
            questObjects = CQS.QuestObjects,
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
        QRL.Dispose();
    }
}