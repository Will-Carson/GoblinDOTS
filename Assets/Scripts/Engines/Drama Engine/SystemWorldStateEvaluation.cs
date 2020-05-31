﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

// TODO Updates and holds the world state datas related to stages
[ServerWorld]
public class SystemWorldStateEvaluation : SystemBase
{

    public NativeArray<DataWorldState> DatasWorldState = new NativeArray<DataWorldState>(G.numberOfStages, Allocator.Persistent);

    [BurstCompile]
    struct SystemWorldStateEvaluationJob : IJob
    {
        // Add fields here that your job needs to do its work.
        // For example,
        //    public float deltaTime;



        public void Execute()
        {
            // Implement the work to perform for each entity here.
            // You should only access data that is local or that is a
            // field on this job. Note that the 'rotation' parameter is
            // marked as [ReadOnly], which means it cannot be modified,
            // but allows this job to run in parallel with other jobs
            // that want to read Rotation component data.
            // For example,
            //     translation.Value += mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;


        }
    }

    protected override void OnUpdate()
    {
        var job = new SystemWorldStateEvaluationJob();

        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        //     job.deltaTime = UnityEngine.Time.deltaTime;



        // Now that the job is set up, schedule it to be run. 
        job.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DatasWorldState.Dispose();
    }
}