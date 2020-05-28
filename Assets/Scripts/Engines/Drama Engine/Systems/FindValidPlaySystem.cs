﻿using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

public class FindValidPlaySystem : SystemBase
{
    [AutoAssign] LocationManagerSystem LMS;
    [AutoAssign] RunPlaySystem RPS;

    private NativeArray<PlayRequirementsLibrary> PRL;

    protected override void OnCreate()
    {
        base.OnCreate();
        PlayRequirementsLibrary p = new PlayRequirementsLibrary()
        {
            playRequirements = new IPlayRequirements[]
            {
                // Define play requirements here
            }
        };
        PRL[0] = p;
    }

    [BurstCompile]
    struct FindValidPlaySystemJob : IJob
    {
        public LocationManagerSystem lms;
        public RunPlaySystem rps;

        public NativeArray<PlayRequirementsLibrary> prl;

        public void Execute()
        {
            // Search through stages for a stage without a play
            int stageId = 0;

            for (int i = 0; i < lms.StageDatas.Length; i++)
            {
                // Set stage id
                // TODO Set stage id
            }

            // Search through plays for one that's applicable to that stage.
            // If none are applicable, play a default non-play that eats up a chunk of time.
            List<EventPlayRequest> validPlayRequests = new List<EventPlayRequest>();
            var playRequest = new EventPlayRequest();
            var pr = prl[0].playRequirements;

            for (int i = 0; i < pr.Length; i++)
            {
                if (pr[i].Requirements(out playRequest))
                {
                    validPlayRequests.Add(playRequest);
                }
            }

            // Send the play request to the RunPlaySystem
            // TODO it's possible that Native* objects Count attribute is never 0. 
            // If this is the case, I'll have to do a different test here. Need to test this.
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
            prl = PRL
        };
        
        job.Schedule();
    }
}