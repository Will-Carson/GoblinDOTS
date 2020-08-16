using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class DramaController : SystemBase
{
    [AutoAssign] protected EndSimulationEntityCommandBufferSystem ESECBS;

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer();
        var timeSlot = InGameDateTime.TimeSlot;

        Entities
        .WithAll<NeedsPlay>()
        .ForEach((
            ref Entity entity,
            in Exhausted exhausted,
            in StageId stageId) =>
        {
            if (exhausted.lastAction == timeSlot) return;
            var s = new Exhausted { lastAction = timeSlot };
            ecb.SetComponent(entity, s);
            var e = ecb.CreateEntity();
            var r = new BuildSituationRequest
            {
                stageId = stageId.value
            };
            ecb.AddComponent(e, r);
        })
        .Run();

        ESECBS.AddJobHandleForProducer(Dependency);
    }
}

public struct Exhausted : IComponentData
{
    public TimeSlot lastAction;
}