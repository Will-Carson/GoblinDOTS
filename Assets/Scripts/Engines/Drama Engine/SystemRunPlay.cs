using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

// TODO PEL has no place here. This system isn't actually running the plays, so the PEL doesn't matter.
[ServerWorld]
public class SystemRunPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    public NativeArray<DataPlayExecution> PEL = new NativeArray<DataPlayExecution>(G.numberOfPlays, Allocator.Persistent);
    public NativeArray<Entity> RunningPlays = new NativeArray<Entity>(G.numberOfStages, Allocator.Persistent);

    protected override void OnCreate()
    {
        PEL[0] = new DataPlayExecution();
    }

    protected override void OnUpdate()
    {
        var trackedEntities = RunningPlays;
        var buffer = ESECBS.CreateCommandBuffer();
        var X = new Entity();

        #region Track running play objects with system 
        Entities
            .WithNone<SystemRunningPlay>()
            .ForEach((Entity entity, in RunningPlay runningPlay, in StageId stageId) =>
            {
                var s = new SystemRunningPlay() { /*set values*/ };
                buffer.AddComponent<SystemRunningPlay>(entity);
                buffer.SetComponent(entity, s);
                trackedEntities[stageId.stageId] = entity;
            })
            .WithBurst()
            .Schedule();

        Entities
            .WithNone<RunningPlay>()
            .ForEach((Entity entity, in SystemRunningPlay system, in StageId stageId) =>
            {
                buffer.RemoveComponent<SystemRunningPlay>(entity);
                trackedEntities[stageId.stageId] = X;
            })
            .WithBurst()
            .Schedule();
        #endregion

        // Update plays as necessary


        // Send play updates to clients
    }

    protected override void OnDestroy()
    {
        PEL.Dispose();
    }
}

public struct SystemRunningPlay : ISystemStateComponentData
{
    public DataRunningPlay runningPlay;
}