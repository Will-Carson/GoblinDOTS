using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class FindValidTaskSystem : SystemBase
{
    [AutoAssign] public LocationManagerSystem LMS;
    [AutoAssign] public RunTaskSystem RTS;
    [AutoAssign] public CharacterStateManagementSystem CSMS;
    [AutoAssign] public WorldStateEvaluationSystem WSES;

    public NativeArray<TaskRequirementsLibrary> TRL = new NativeArray<TaskRequirementsLibrary>();

    protected override void OnCreate()
    {
        TaskRequirementsLibrary trl = new TaskRequirementsLibrary()
        {
            taskRequirements = new ITaskRequirement[]
            {
                // Define task requirements here
                // TODO write task requirements
            }
        };
        TRL[0] = trl;
    }

    [BurstCompile]
    struct FindValidTaskSystemJob : IJob
    {
        public LocationManagerSystem lms;
        public RunTaskSystem rts;
        public CharacterStateManagementSystem csms;
        public WorldStateEvaluationSystem wses;

        public NativeArray<TaskRequirementsLibrary> trl;

        public void Execute()
        {
            // Loop through every character looking for ones that aren't busy
            NativeList<int> lazyCharacters = new NativeList<int>();
            for (int i = 0; i < csms.CharacterStates.Count(); i++)
            {
                if (csms.CharacterStates[i] == CharacterState.waitingForTask)
                {
                    lazyCharacters.Add(i);
                }
            }

            // Assign non-busy characters tasks
            NativeList<EventTaskRequest> validTasks = new NativeList<EventTaskRequest>();
            EventTaskRequest eventTaskRequest = new EventTaskRequest();
            var rl = trl[0].taskRequirements;
            
            for (int i = 0; i < lazyCharacters.Length; i++)
            {
                for (int j = 0; j < rl.Length; j++)
                {
                    var worldState = wses.worldStateDatas[lms.characterLocations[i].siteId];
                    if (rl[j].Requirements(out eventTaskRequest, worldState))
                    {
                        validTasks.Add(eventTaskRequest);
                    }
                }
                if (validTasks.Length == 0)
                {
                    // Send default task
                    rts.EventsTaskRequest.Add(new EventTaskRequest()
                    {
                        characterId = lazyCharacters[i],
                        pointId = 0,
                        taskId = 0
                    });
                }
                else
                {
                    // Send valid task
                    // For now just sending the first valid task. TODO find a better way
                    rts.EventsTaskRequest.Add(validTasks[0]);
                }
            }
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new FindValidTaskSystemJob()
        {
            csms = CSMS,
            lms = LMS,
            rts = RTS,
            trl = TRL
        };

        job.Schedule();
    }
}