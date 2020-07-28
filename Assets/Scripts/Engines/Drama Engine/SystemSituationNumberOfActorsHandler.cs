using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class SystemSituationNumberOfActorsHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    private NativeHashMap<int, int> StageData = new NativeHashMap<int, int>(G.maxNPCPopulation, Allocator.TempJob);

    protected override void OnCreate()
    {
        Buffer = ESECBS.CreateCommandBuffer().ToConcurrent();
    }

    protected override void OnDestroy()
    {
        StageData.Dispose();
    }

    private EntityCommandBuffer.Concurrent Buffer;

    protected override void OnUpdate()
    {
        var buffer = Buffer;
        var stageData = StageData;

        Entities.ForEach((Entity entity, StageId stageId, DynamicBuffer<Occupant> occupants) => {
            stageData.Add(stageId.value, occupants.Length);
        })
        .WithBurst()
        .Schedule();

        Entities.ForEach((Entity entity, int entityInQueryIndex, PartialSituation situation, NeedsNumberOfActors need, DynamicBuffer<StageParameters> parameters) =>
        {
            buffer.RemoveComponent(entityInQueryIndex, entity, need.GetType());
            int v;
            stageData.TryGetValue(situation.stageId, out v);

            var p = new StageParameters()
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

        stageData.Clear();
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