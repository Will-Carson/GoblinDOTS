using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;
using System.Collections.Generic;
using System.Linq;
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
            IEnumerable<DataStage> validStages =
                from stage in datasStage
                where stage.state == TypeStageState.notBusy
                select stage;

            if (validStages.Count() == 0) return;

            var e = new EventPlayRequest();
            foreach (var vs in validStages)
            {
                var ws = dws[vs.id];
                IEnumerable<EventPlayRequest> validPlays =
                    from pr in prl
                    where Requirements(pr, out e, ws)
                    select e;

                if (validPlays.Count() == 0) epr.Add(new EventPlayRequest(0, vs.id));
                else epr.Add(validPlays.First());
            }
        }

        private static bool Requirements(PlayRequirement playReq, out EventPlayRequest pr, DataWorldState ws)
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