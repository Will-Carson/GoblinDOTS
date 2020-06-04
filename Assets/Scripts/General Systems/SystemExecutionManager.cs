using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

// Define world i.e., [ServerWorld, ClientWorld]
// Define relationships i.e., [UpdateBefore(typeof(BuildPhysicsWorld))]
// Define group name i.e., public class ApplyPhysicsGroup : ComponentSystemGroup {}

//[ServerWorld]
//public class ExecuteDrama : ComponentSystemGroup { }

[ServerWorld]
public class SystemExecutionManager : SystemBase
{
    private int frame = 0;

    [AutoAssign] SystemCheckQuest CheckQuest;
    [AutoAssign] SystemFindValidDialogue FindValidDialogue;
    [AutoAssign] SystemFindValidPlay FindValidPlay;
    [AutoAssign] SystemFindValidQuest FindValidQuest;
    [AutoAssign] SystemFindValidTask FindValidTask;
    [AutoAssign] SystemLocationManager LocationManager;
    [AutoAssign] SystemRunDialogue RunDialogue;
    [AutoAssign] SystemRunPlay RunPlay;
    [AutoAssign] SystemRunTask RunTask;
    [AutoAssign] SystemWorldStateEvaluation WorldStateEvaluation;
    [AutoAssign] SystemManageFactionAndFactionMembers ManageFactionAndFactionMembers;
    [AutoAssign] SystemProcessDeedOrRumorEvent ProcessDeedOrRumorEvent;
    [AutoAssign] SystemBuildLocationData BuildLocationData;

    JobHandle CheckQuestHandle;
    JobHandle FindValidDialogueHandle;
    JobHandle FindValidPlayHandle;
    JobHandle FindValidQuestHandle;
    JobHandle FindValidTaskHandle;
    JobHandle LocationManagerHandle;
    JobHandle RunDialogueHandle;
    JobHandle RunPlayHandle;
    JobHandle RunTaskHandle;
    JobHandle WorldStateEvaluationHandle;
    JobHandle ManageFactionAndFactionMembersHandle;
    JobHandle ProcessDeedOrRumorEventHandle;
    JobHandle BuildLocationDataHandle;

    JobHandle LocationManagerReqs;

    protected override void OnCreate()
    {
        NativeArray<JobHandle> SLMReqs = new NativeArray<JobHandle>(2, Allocator.Temp);
        SLMReqs[0] = FindValidQuestHandle;
        SLMReqs[1] = FindValidTaskHandle;
        LocationManagerReqs = JobHandle.CombineDependencies(SLMReqs);

        SLMReqs.Dispose();
    }

    protected override void OnUpdate()
    {
        frame++;
        
        // Every odd frame complete all jobs
        if (frame - 1 % 2 == 0)
        {
            CheckQuestHandle.Complete();
            FindValidDialogueHandle.Complete();
            FindValidPlayHandle.Complete();
            FindValidQuestHandle.Complete();
            FindValidTaskHandle.Complete();
            LocationManagerHandle.Complete();
            RunDialogueHandle.Complete();
            RunPlayHandle.Complete();
            RunTaskHandle.Complete();
            WorldStateEvaluationHandle.Complete();
            ManageFactionAndFactionMembersHandle.Complete();
            ProcessDeedOrRumorEventHandle.Complete();
            BuildLocationDataHandle.Complete();
        }

        // AI Phase 1
        if (frame - 0 % 10 == 0)
        {
            CheckQuestHandle = CheckQuest.ScheduleEvent();
            FindValidDialogueHandle = FindValidDialogue.ScheduleEvent();
            FindValidPlayHandle = FindValidPlay.ScheduleEvent();
            FindValidQuestHandle = FindValidQuest.ScheduleEvent();
            FindValidTaskHandle = FindValidTask.ScheduleEvent();
            RunDialogueHandle = RunDialogue.ScheduleEvent();
            RunPlayHandle = RunPlay.ScheduleEvent(FindValidPlayHandle);
            RunTaskHandle = RunTask.ScheduleEvent(FindValidTaskHandle);
            WorldStateEvaluationHandle = WorldStateEvaluation.ScheduleEvent();
            ManageFactionAndFactionMembersHandle = ManageFactionAndFactionMembers.ScheduleEvent();
            ProcessDeedOrRumorEventHandle = ProcessDeedOrRumorEvent.ScheduleEvent();
            BuildLocationDataHandle = BuildLocationData.ScheduleEvent();
        }

        // AI Phase 2
        if (frame - 8 % 10 == 0)
        {
            LocationManagerHandle = LocationManager.ScheduleEvent(LocationManagerReqs);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CheckQuestHandle.Complete();
        FindValidDialogueHandle.Complete();
        FindValidPlayHandle.Complete();
        FindValidQuestHandle.Complete();
        FindValidTaskHandle.Complete();
        LocationManagerHandle.Complete();
        RunDialogueHandle.Complete();
        RunPlayHandle.Complete();
        RunTaskHandle.Complete();
        WorldStateEvaluationHandle.Complete();
        ManageFactionAndFactionMembersHandle.Complete();
        ProcessDeedOrRumorEventHandle.Complete();
        BuildLocationDataHandle.Complete();
    }
}