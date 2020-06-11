using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;
using System;

public class SystemTemplate : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    public NativeHashMap<Entity, ExampleSystemComponent> TrackedEntities = 
        new NativeHashMap<Entity, ExampleSystemComponent>(200, Allocator.Persistent);

    [BurstCompile]
    struct ExampleJob : IJob
    {
        public NativeHashMap<Entity, ExampleSystemComponent> trackedEntities;

        public void Execute()
        {
            var keys = trackedEntities.GetKeyArray(Allocator.Persistent);
            foreach (var key in keys)
            {
                trackedEntities[key] = new ExampleSystemComponent() { Value = trackedEntities[key].Value + 1 };
            }
        }
    }

    protected override void OnUpdate()
    {
        // Add system state component to facilitate tracking
        var trackedEntities = TrackedEntities;
        var buffer = ESECBS.CreateCommandBuffer();

        Entities
            .WithNone<ExampleSystemComponent>()
            .ForEach((Entity entity, in ExampleComponent exampleComponent) =>
            {
                var e = new ExampleSystemComponent() { Value = exampleComponent.Value };
                buffer.AddComponent<ExampleSystemComponent>(entity);
                buffer.SetComponent<ExampleSystemComponent>(entity, e);
                trackedEntities.Add(entity, e);
            })
            .WithBurst()
            .Schedule();

        // Do operations on all valid data in the meantime
        var job = new ExampleJob()
        {
            trackedEntities = TrackedEntities
        };
        job.Schedule();

        // Destroy entities without real components and stop tracking them
        Entities
            .WithNone<ExampleComponent>()
            .ForEach((Entity entity, in ExampleSystemComponent exampleSystemComponent) =>
            {
                trackedEntities.Remove(entity);
                buffer.DestroyEntity(entity);
            })
            .WithBurst()
            .Schedule();
    }
}

public struct ExampleComponent : IComponentData
{
    public int Value;
}

public struct ExampleSystemComponent : ISystemStateComponentData
{
    public int Value;
}