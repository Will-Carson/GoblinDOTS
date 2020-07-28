using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class SystemSituationNumberOfActorsHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    protected override void OnCreate()
    {
        
    }

    protected override void OnDestroy()
    {

    }

    private EntityCommandBuffer.Concurrent buffer;

    protected override void OnUpdate()
    {
        buffer = ESECBS.CreateCommandBuffer().ToConcurrent();

        // Not sure if entityInQueryIndex will work for jobchunk id.
        Entities.ForEach((Entity entity, int entityInQueryIndex, NeedsNumberOfActors need, DynamicBuffer<ParameterBuffer> parameters) =>
        {
            // Do stuff
        })
        .WithBurst()
        .Schedule();
    }
}