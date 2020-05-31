using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemFindValidTask<TR> : SystemBase 
    where TR : struct, ITaskRequirement
{
    [AutoAssign] public SystemLocationManager LMS;
    [AutoAssign] public SystemRunTask RTS;
    [AutoAssign] public SystemCharacterStateManagement CSMS;
    [AutoAssign] public SystemWorldStateEvaluation WSES;

    private NativeArray<TR> TRL;

    protected override void OnCreate()
    {
        TRL = new NativeArray<TR>(GlobalVariables.numberOfTasks, Allocator.Persistent)
        {
            // TODO the thing
        };
    }

    [BurstCompile]
    struct SystemFindValidTaskJob : IJob
    {
        public SystemLocationManager lms;
        public SystemRunTask rts;
        public SystemCharacterStateManagement csms;
        public SystemWorldStateEvaluation wses;

        public NativeArray<TR> trl;

        public void Execute()
        {
            // Loop through every character looking for ones that aren't busy
            NativeList<int> lazyCharacters = new NativeList<int>(GlobalVariables.maxLazyCharacters, Allocator.Temp);
            for (int i = 0; i < csms.CharacterStates.Count(); i++)
            {
                if (csms.CharacterStates[i] == CharacterState.waitingForTask)
                {
                    lazyCharacters.Add(i);
                }
            }

            // Assign non-busy characters tasks
            NativeList<EventTaskRequest> validTasks = new NativeList<EventTaskRequest>(GlobalVariables.maxValidTasks, Allocator.Temp);
            EventTaskRequest eventTaskRequest = new EventTaskRequest();
            
            for (int i = 0; i < lazyCharacters.Length; i++)
            {
                for (int j = 0; j < trl.Length; j++)
                {
                    var worldState = wses.DatasWorldState[lms.CharacterLocations[i].siteId];
                    if (trl[j].Requirements(out eventTaskRequest, worldState))
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

                // Dispose of temp Native*'s
                lazyCharacters.Dispose();
                validTasks.Dispose();
            }
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new SystemFindValidTaskJob()
        {
            csms = CSMS,
            lms = LMS,
            rts = RTS,
            trl = TRL
        };

        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        TRL.Dispose();
    }
}