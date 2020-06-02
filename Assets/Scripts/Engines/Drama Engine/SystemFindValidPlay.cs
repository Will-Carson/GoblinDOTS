using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemFindValidPlay<PR, PE> : SystemBase 
    where PR : unmanaged, IPlayRequirement 
    where PE : unmanaged, IPlayExecution
{
    [AutoAssign] SystemLocationManager LMS;
    [AutoAssign] SystemRunPlay<PE> RPS;
    [AutoAssign] SystemWorldStateEvaluation WSES;
    private NativeArray<PR> PRL = new NativeArray<PR>(G.numberOfPlays, Allocator.Persistent);

    protected override void OnCreate()
    {
        dynamic p;
        PRL[0] = p = new PlayRDefault();
    }

    [BurstCompile]
    struct SystemFindValidPlayJob : IJob
    {
        public SystemLocationManager lms;
        public SystemRunPlay<PE> rps;
        public SystemWorldStateEvaluation wses;

        public NativeArray<PR> prl;

        public void Execute()
        {
            // Search through stages for a stage without a play
            int stageId = 0;

            for (int i = 0; i < lms.StageDatas.Length; i++)
            {
                // Set stage id
                // TODO Set stage id more selectively.
                if (lms.StageDatas[i].state == TypeStageState.notBusy)
                {
                    stageId = i;
                }
            }

            // Search through plays for one that's applicable to that stage.
            // If none are applicable, play a default non-play that eats up a chunk of time.
            var validPlayRequests = new NativeList<EventPlayRequest>(G.numberOfStages, Allocator.Temp);
            var playRequest = new EventPlayRequest();
            var worldState = wses.DatasWorldState[stageId];

            for (int i = 0; i < prl.Length; i++)
            {
                if (prl[i].Requirements(out playRequest, worldState))
                {
                    validPlayRequests.Add(playRequest);
                }
            }

            // Send the play request to the RunPlaySystem
            if (validPlayRequests.Length == 0)
            {
                // If no valid plays, send a default play request
                rps.EventsPlayRequest.Add(new EventPlayRequest()
                {
                    playId = 0,
                    stageId = stageId
                });
            }
            else
            {
                // Otherwise send a valid play request
                // TODO just sending the first valid play request right now. This may not be the way to do it.
                rps.EventsPlayRequest.Add(validPlayRequests[0]);
            }

            // Dispose
            validPlayRequests.Dispose();
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new SystemFindValidPlayJob()
        {
            lms = LMS,
            rps = RPS,
            prl = PRL,
            wses = WSES
        };
        
        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PRL.Dispose();
    }
}