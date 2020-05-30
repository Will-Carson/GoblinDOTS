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
public class FindValidPlaySystem : SystemBase
{
    [AutoAssign] LocationManagerSystem LMS;
    [AutoAssign] RunPlaySystem RPS;
    [AutoAssign] WorldStateEvaluationSystem WSES;

    private NativeArray<PlayRequirementsLibrary> PRL;

    protected override void OnCreate()
    {
        base.OnCreate();
        PlayRequirementsLibrary p = new PlayRequirementsLibrary()
        {
            playRequirements = new IPlayRequirement[]
            {
                // Define play requirements here
                // TODO define play requirements
            }
        };
        PRL[0] = p;
    }

    [BurstCompile]
    struct FindValidPlaySystemJob : IJob
    {
        public LocationManagerSystem lms;
        public RunPlaySystem rps;
        public WorldStateEvaluationSystem wses;

        public NativeArray<PlayRequirementsLibrary> prl;

        public void Execute()
        {
            // Search through stages for a stage without a play
            int stageId = 0;

            for (int i = 0; i < lms.StageDatas.Length; i++)
            {
                // Set stage id
                // TODO Set stage id more selectively.
                if (lms.StageDatas[i].state == StageState.notBusy)
                {
                    stageId = i;
                }
            }

            // Search through plays for one that's applicable to that stage.
            // If none are applicable, play a default non-play that eats up a chunk of time.
            List<EventPlayRequest> validPlayRequests = new List<EventPlayRequest>();
            var playRequest = new EventPlayRequest();
            var worldState = wses.WorldStateDatas[stageId];
            var pr = prl[0].playRequirements;

            for (int i = 0; i < pr.Length; i++)
            {
                if (pr[i].Requirements(out playRequest, worldState))
                {
                    validPlayRequests.Add(playRequest);
                }
            }

            // Send the play request to the RunPlaySystem
            if (validPlayRequests.Count == 0)
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
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new FindValidPlaySystemJob()
        {
            lms = LMS,
            rps = RPS,
            prl = PRL,
            wses = WSES
        };
        
        job.Schedule();
    }
}