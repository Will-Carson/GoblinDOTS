using System.Collections.Generic;
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

    private NativeArray<PlayRequirementsLibrary> PlayRequirementsLibrary;

    [BurstCompile]
    struct FindValidPlaySystemJob : IJob
    {
        // Add fields here that your job needs to do its work.
        // For example,
        //    public float deltaTime;
        public LocationManagerSystem lms;
        public RunPlaySystem rps;

        public NativeArray<PlayRequirementsLibrary> playRequirementsLibrary;

        public void Execute()
        {
            // Search through stages for a stage without a play
            int stageId = 0;

            for (int i = 0; i < lms.StageDatas.Length; i++)
            {
                // Set stage id
            }

            // Search through plays for one that's applicable to that stage.
            // If none are applicable, play a default non-play that eats up a chunk of time.
            List<int> validPlayIds = new List<int>();
            var pr = playRequirementsLibrary[0].playRequirements;

            for (int i = 0; i < pr.Length; i++)
            {
                if (pr[i].Requirements())
                {
                    validPlayIds.Add(i);
                }
            }

            // Send the play request to the RunPlaySystem
            if (validPlayIds.Count == 0)
            {
                rps.EventsPlayRequest.Add(new EventPlayRequest()
                {
                    playId = 0,
                    stageId = stageId
                });
            }
            else
            {
                rps.EventsPlayRequest.Add(new EventPlayRequest()
                {
                    playId = validPlayIds[0], // Just putting in the first play in the list. May be more selective later.
                    stageId = stageId
                });
            }
        }
    }
    
    protected override void OnUpdate()
    {
        var job = new FindValidPlaySystemJob()
        {
            lms = LMS,
            rps = RPS,
            playRequirementsLibrary = PlayRequirementsLibrary
        };
        
        job.Schedule();
    }
}