using System.Collections.Generic;
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
public class SystemFindValidPlay : SystemBase
{
    [AutoAssign] SystemLocationManager LMS;
    [AutoAssign] SystemRunPlay RPS;
    [AutoAssign] SystemWorldStateEvaluation WSES;
    private NativeArray<PlayRequirement> PRL = new NativeArray<PlayRequirement>(G.numberOfPlays, Allocator.Persistent);

    protected override void OnCreate()
    {
        PRL[0] = new PlayRequirement();
    }

    [BurstCompile]
    struct SystemFindValidPlayJob : IJob
    {
        public NativeList<EventPlayRequest> epr;
        [ReadOnly] public NativeArray<DataWorldState> dws;
        [ReadOnly] public NativeArray<DataStage> datasStage;
        [ReadOnly] public NativeArray<PlayRequirement> prl;

        public void Execute()
        {
            // Search through stages for a stage without a play
            int stageId = 0;

            for (int i = 0; i < datasStage.Length; i++)
            {
                // Set stage id
                // TODO Set stage id more selectively.
                if (datasStage[i].state == TypeStageState.notBusy)
                {
                    stageId = i;
                }
            }

            // Search through plays for one that's applicable to that stage.
            // If none are applicable, play a default non-play that eats up a chunk of time.
            var validPlayRequests = new NativeList<EventPlayRequest>(G.numberOfStages, Allocator.Temp);
            var playRequest = new EventPlayRequest();
            var worldState = dws[stageId];

            for (int i = 0; i < prl.Length; i++)
            {
                if (Requirements(prl[i], out playRequest, worldState))
                {
                    validPlayRequests.Add(playRequest);
                }
            }

            // Send the play request to the RunPlaySystem
            if (validPlayRequests.Length == 0)
            {
                // If no valid plays, send a default play request
                epr.Add(new EventPlayRequest()
                {
                    playId = 0,
                    stageId = stageId
                });
            }
            else
            {
                // Otherwise send a valid play request
                // TODO just sending the first valid play request right now. This may not be the way to do it.
                epr.Add(validPlayRequests[0]);
            }

            // Dispose
            validPlayRequests.Dispose();
        }

        private bool Requirements(PlayRequirement playReq, out EventPlayRequest pr, DataWorldState ws)
        {
            pr = new EventPlayRequest();
            return false;
        }
    }
    
    protected override void OnUpdate()
    {
        
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PRL.Dispose();
    }

    public JobHandle ScheduleEvent()
    {
        var job = new SystemFindValidPlayJob()
        {
            datasStage = LMS.StageDatas,
            dws = WSES.DatasWorldState,
            epr = RPS.EventsPlayRequest,
            prl = PRL
        };

        return job.Schedule();
    }
}