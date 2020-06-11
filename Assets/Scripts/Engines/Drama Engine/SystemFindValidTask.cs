//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Transforms;
//using static Unity.Mathematics.math;
//using DOTSNET;
//using System;

//[ServerWorld]
//public class SystemFindValidTask : SystemBase 
//{
//    [AutoAssign] public SystemLocationManager LMS;
//    [AutoAssign] public SystemRunTask RTS;
//    [AutoAssign] public SystemCharacterStateManagement CSMS;
//    [AutoAssign] public SystemWorldStateEvaluation WSES;

//    private NativeArray<TaskRequirement> TRL = new NativeArray<TaskRequirement>(G.numberOfTasks, Allocator.Persistent);

//    protected override void OnCreate()
//    {
//        // Example of assigning a task
//        TRL[0] = new TaskRequirement();
//    }

//    [BurstCompile]
//    struct SystemFindValidTaskJob : IJob
//    {
//        [ReadOnly] public NativeHashMap<int, DataLocation> ld;
//        public NativeList<EventTaskRequest> ets;
//        [ReadOnly] public NativeHashMap<int, CharacterState> cs;
//        [ReadOnly] public NativeArray<DataWorldState> dws;
//        [ReadOnly] public NativeArray<TaskRequirement> trl;

//        public void Execute()
//        {
//            // Loop through every character looking for ones that aren't busy
//            NativeList<int> lazyCharacters = new NativeList<int>(G.maxLazyCharacters, Allocator.Temp);
//            for (int i = 0; i < cs.Count(); i++)
//            {
//                if (cs[i] == CharacterState.waitingForTask)
//                {
//                    lazyCharacters.Add(i);
//                }
//            }

//            // Assign non-busy characters tasks
//            NativeList<EventTaskRequest> validTasks = new NativeList<EventTaskRequest>(G.maxValidTasks, Allocator.Temp);
//            EventTaskRequest eventTaskRequest = new EventTaskRequest();
            
//            for (int i = 0; i < lazyCharacters.Length; i++)
//            {
//                for (int j = 0; j < trl.Length; j++)
//                {
//                    var worldState = dws[ld[i].siteId];
//                    if (Requirements(trl[j], out eventTaskRequest, worldState))
//                    {
//                        validTasks.Add(eventTaskRequest);
//                    }
//                }
//                if (validTasks.Length == 0)
//                {
//                    // Send default task
//                    ets.Add(new EventTaskRequest()
//                    {
//                        characterId = lazyCharacters[i],
//                        pointId = 0,
//                        taskId = 0
//                    });
//                }
//                else
//                {
//                    // Send valid task
//                    // For now just sending the first valid task. TODO find a better way
//                    ets.Add(validTasks[0]);
//                }

//                // Dispose
//                lazyCharacters.Dispose();
//                validTasks.Dispose();
//            }
//        }

//        private bool Requirements(TaskRequirement taskReq, out EventTaskRequest tr, DataWorldState ws)
//        {
//            tr = new EventTaskRequest();
//            return false;
//        }
//    }
    
//    protected override void OnUpdate()
//    {
//        var job = new SystemFindValidTaskJob()
//        {
//            cs = CSMS.CharacterStates,
//            dws = WSES.DatasWorldState,
//            ets = RTS.EventsTaskRequest,
//            ld = LMS.CharacterLocations,
//            trl = TRL
//        };

//        job.Schedule();
//    }

//    protected override void OnDestroy()
//    {
//        base.OnDestroy();
//        TRL.Dispose();
//    }
//}