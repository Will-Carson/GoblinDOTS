using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class SituationNumberOfActorsHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    protected override void OnUpdate()
    {
        var buffer = ESECBS.CreateCommandBuffer().AsParallelWriter();
        var stageData = new NativeHashMap<int, int>(G.maxNPCPopulation, Allocator.Persistent);

        Entities.ForEach((
            in Entity entity,
            in StageId stageId,
            in DynamicBuffer<Occupant> occupants) => {
            stageData.TryAdd(stageId.value, occupants.Length);
        })
        .WithBurst()
        .Run();

        Entities.ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in PartialSituation situation,
            in NeedsNumberOfActors need,
            in DynamicBuffer<SituationParameters> parameters) =>
        {
            buffer.RemoveComponent<NeedsNumberOfActors>(entityInQueryIndex, entity);
            int v;
            stageData.TryGetValue(situation.stageId, out v);

            var p = new SituationParameters()
            {
                param = new Parameter()
                {
                    op = Operator.Equal,
                    type = ParameterType.NumberOfActors,
                    value1 = v
                }
            };

            buffer.AppendToBuffer(entityInQueryIndex, entity, p);
        })
        .WithBurst()
        .Schedule();

        ESECBS.AddJobHandleForProducer(Dependency);

        Dependency.Complete();
        stageData.Dispose();
    }
}

public struct StageId : IComponentData
{
    public int value;
}

public struct Occupant : IBufferElementData
{
    public int id;
    public Entity occupant;
}