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
        Entities
            .WithAll<ExampleComponent>()
            .WithNone<ExampleSystemComponent>()
            .ForEach((Entity entity, in ExampleComponent exampleComponent, in ExampleSystemComponent exampleSystemComponent) =>
            {
                ESECBS.EntityManager.AddComponentData(entity, new ExampleSystemComponent() { Value = exampleComponent.Value });
                TrackedEntities.Add(entity, exampleSystemComponent);
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
            .WithAll<ExampleSystemComponent>()
            .ForEach((Entity entity, in ExampleComponent exampleComponent, in ExampleSystemComponent exampleSystemComponent) =>
            {
                
                ESECBS.EntityManager.DestroyEntity(entity);
                TrackedEntities.Remove(entity);
            })
            .WithBurst()
            .Schedule();
    }
}

public struct EntityTag : IComponentData
{
    public Entity myEntity;
}

public struct ExampleComponent : IComponentData
{
    public int Value;
}

public struct ExampleSystemComponent : ISystemStateComponentData
{
    public int Value;
}