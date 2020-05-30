using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class FindValidQuestSystem : SystemBase
{
    [AutoAssign] LocationManagerSystem LMS;
    [AutoAssign] CheckQuestSystem CQS;
    [AutoAssign] WorldStateEvaluationSystem WSES;

    public NativeList<EventQuestRequest> EventsQuestRequest = new NativeList<EventQuestRequest>();

    private NativeArray<QuestRequirementsLibrary> QRL = new NativeArray<QuestRequirementsLibrary>(1, Allocator.Persistent);

    public NativeHashMap<EventQuestRequest, ValidQuest> CurrentQuests = new NativeHashMap<EventQuestRequest, ValidQuest>();

    protected override void OnCreate()
    {
        base.OnCreate();
        var qrl = new QuestRequirementsLibrary()
        {
            questRequirements = new IQuestRequirements[]
            {
                // Define quest requirements here
                // TODO define quest requirements
            }
        };
        QRL[0] = qrl;
    }

    [BurstCompile]
    struct FindValidQuestSystemJob : IJob
    {
        public LocationManagerSystem lms;
        public CheckQuestSystem cqs;
        public WorldStateEvaluationSystem wses;
        public NativeList<EventQuestRequest> eventsQuestRequest;
        private NativeArray<QuestRequirementsLibrary> qrl;
        public NativeHashMap<EventQuestRequest, ValidQuest> currentQuests;

        public void Execute()
        {
            var validQuest = new ValidQuest();
            var validQuests = new NativeList<ValidQuest>();
            var qr = qrl[0].questRequirements;
            for (int i = 0; i < eventsQuestRequest.Length; i++)
            {
                var e = eventsQuestRequest[i];
                for (int j = 0; j < qr.Length; j++)
                {
                    qr[j].Requirements(out validQuest, wses.WorldStateDatas[lms.CharacterLocations[e.giverId].stageId]);
                    if (validQuest.questId != 0)
                    {
                        validQuests.Add(validQuest);
                    }
                }

                // Just adding the first quest to the current quests list. TODO do something more interesting.
                currentQuests.Add(e, validQuests[0]);
            }
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new FindValidQuestSystemJob()
        {
            cqs = CQS,
            currentQuests = CurrentQuests,
            eventsQuestRequest = EventsQuestRequest,
            lms = LMS,
            wses = WSES
        };

        job.Schedule();
    }
}