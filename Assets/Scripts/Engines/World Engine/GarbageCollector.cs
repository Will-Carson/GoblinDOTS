using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

//[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
[ServerWorld, ClientWorld]
public class GarbageCollector : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;
    
    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().AsParallelWriter();

        Entities
        .ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in Garbage garbage) => 
        {
            ecb.DestroyEntity(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);
    }
}

public struct Garbage : IComponentData { }