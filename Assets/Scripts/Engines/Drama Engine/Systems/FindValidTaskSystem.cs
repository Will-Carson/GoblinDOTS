using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

public class FindValidTaskSystem : SystemBase
{
    [AutoAssign] public LocationManagerSystem LMS;
    [AutoAssign] public RunTaskSystem RTS;
    [AutoAssign] public CharacterStateManagementSystem CSMS;

    public NativeArray<TaskRequirementsLibrary> TaskRequirementsLibrary = new NativeArray<TaskRequirementsLibrary>();

    protected override void OnCreate()
    {
        base.OnCreate();
        TaskRequirementsLibrary = new NativeArray<TaskRequirementsLibrary>()
        {
            // Define task requirements here
            // TODO do the thing
        };
    }

    [BurstCompile]
    struct FindValidTaskSystemJob : IJob
    {
        public LocationManagerSystem lms;
        public RunTaskSystem rts;
        public CharacterStateManagementSystem csms;

        public NativeArray<TaskRequirementsLibrary> taskRequirementsLibrary;

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
            EventTaskRequest eventTaskRequest = new EventTaskRequest();
            NativeList<EventTaskRequest> validTasks = new NativeList<EventTaskRequest>();
            for (int i = 0; i < lazyCharacters.Length; i++)
            {
                for (int j = 0; j < taskRequirementsLibrary[0].taskRequirements.Length; j++)
                {
                    if (taskRequirementsLibrary[0].taskRequirements[j].Requirements(out eventTaskRequest))
                    {
                        validTasks.Add(eventTaskRequest);
                    }
                }
            }

            if (validTasks.Length == 0)
            {
                // Send default task
            }
            else
            {
                // Send valid task
            }
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new FindValidTaskSystemJob()
        {

        };

        job.Schedule();
    }
}