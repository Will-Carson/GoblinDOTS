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
    [AutoAssign] SystemLocationManager SLM;
    [AutoAssign] SystemRunPlay SRP;
    [AutoAssign] SystemWorldStateEvaluation SWSE;
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
                var fr = GetFullRelationships(ws);
                var tm = GetMemoryTemplates(ws);
                var vid = GetValuesByIds(ws);
                IEnumerable<EventPlayRequest> validPlays =
                    from pr in prl
                    where Requirements(pr, out e, ws, fr, tm, vid)
                    select e;

                if (validPlays.Count() == 0) epr.Add(new EventPlayRequest() { playId = 0, stageId = vs.id });
                else epr.Add(validPlays.First());
            }
        }

        private NativeHashMap<int, DataValues> GetValuesByIds(DataWorldState ws)
        {
            throw new NotImplementedException();
        }

        private NativeArray<TemplateMemory> GetMemoryTemplates(DataWorldState ws)
        {
            // TODO
            throw new NotImplementedException();
        }

        private NativeArray<FullRelationship> GetFullRelationships(DataWorldState ws)
        {
            // TODO get relationships from a particular area
            throw new NotImplementedException();
        }

        /// <summary>
        /// Takes a world state and a plays requirements and sees if they're compatable.
        /// If they are, returns true and produces a valid EventPlayRequest.
        /// </summary>
        /// <param name="pr"></param>
        /// <param name="playReq"></param>
        /// <param name="ws"></param>
        /// <param name="fr"></param>
        /// <returns></returns>
        private static bool Requirements(PlayRequirement pr, out EventPlayRequest playReq, DataWorldState ws, NativeArray<FullRelationship> frs, NativeArray<TemplateMemory> tms, NativeHashMap<int, DataValues> vid)
        {
            playReq = new EventPlayRequest(){ playId = pr.playId, stageId = ws.stageId };
            var vpr = new NativeList<PlayRequirement>(10, Allocator.TempJob);

            // Populate list with items meeting one of the constraints
            foreach (var fr in frs)
            {
                var nvpr = new PlayRequirement();
                nvpr.TryAddFullRelationship(1, fr, pr.relationshipX);
                vpr.Add(nvpr);
            }
            
            // Check if there are any items that meet the constraint. If not, this play is invalid.
            if (vpr.Count() == 0) return false;

            // Remove items from the list that do not meet another constraint
            for (var i = vpr.Count() - 1; i >= 0; i--)
            {
                for (var j = 0; j < frs.Length; j++)
                {
                    if (!vpr[i].TryAddFullRelationship(2, frs[j], pr.relationshipY))
                        vpr.RemoveAt(i);
                }
            }

            // Every time we get done removing items we check again
            if (vpr.Count() == 0) return false;

            // Repeat...
            for (var i = vpr.Count() - 1; i >= 0; i--)
            {
                var test = true;
                if (vpr[i].CheckValuesInRange(1, vid[vpr[i].subjectX], pr.cXValues)) test = false;
                if (vpr[i].CheckValuesInRange(2, vid[vpr[i].subjectY], pr.cYValues)) test = false;
                if (vpr[i].CheckValuesInRange(3, vid[vpr[i].subjectZ], pr.cZValues)) test = false;
                if (!test) vpr.RemoveAt(i);
            }

            if (vpr.Count() == 0) return false;

            for (var i = vpr.Count() - 1; i >= 0; i--)
            {
                var test = false;
                for (var j = 0; j < tms.Length; j++)
                {
                    if (vpr[i].CheckValidMemory(tms[j], pr.templateMemory)) test = true;
                }
                if (!test)
                {
                    vpr.RemoveAt(i);
                }
            }

            if (vpr.Count() == 0) return false;

            // Set remaining play request data before returning.
            playReq.subjectX = vpr[0].subjectX;
            playReq.subjectY = vpr[0].subjectY;
            playReq.subjectZ = vpr[0].subjectZ;

            // If all constraints are met and we have any items left over, return true.
            return true;
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
            datasStage = SLM.StageDatas,
            dws = SWSE.DatasWorldState,
            epr = SRP.EventsPlayRequest,
            prl = PRL
        };

        return job.Schedule();
    }
}