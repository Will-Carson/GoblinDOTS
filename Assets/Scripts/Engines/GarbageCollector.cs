using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

//[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
[ServerWorld, ClientWorld]
public class GarbageCollector : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    
    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().ToConcurrent();

        Entities
        .ForEach((Entity entity, int entityInQueryIndex, Garbage garbage) => 
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