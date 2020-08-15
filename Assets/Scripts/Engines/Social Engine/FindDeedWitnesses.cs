using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class FindDeedWitnesses : SystemBase
{
    [AutoAssign] protected EndSimulationEntityCommandBufferSystem ESECBS;
    
    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().ToConcurrent();

        Entities
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            DynamicBuffer<FindDeedWitnessesRequest> requests,
            DynamicBuffer<Occupant> occupants) =>
        {
            for (int i = 0; i < requests.Length; i++)
            {
                for (int j = 0; j < occupants.Length; j++)
                {
                    var witnessedEvent = new WitnessedEvent
                    {
                        deedDoerFactionMemberId = requests[i].deedDoerFactionMemberId,
                        deedTargetFactionMemberId = requests[i].deedTargetFactionMemberId,
                        deedWitnessFactionMemberId = occupants[j].id,
                        isRumor = false,
                        needsEvaluation = true,
                        reliability = 1,
                        rumorSpreaderFactionMemberId = 0,
                        type = requests[i].type
                    };
                    ecb.AppendToBuffer(entityInQueryIndex, occupants[j].occupant, witnessedEvent);
                }
            }
            ecb.SetBuffer<FindDeedWitnessesRequest>(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        ESECBS.AddJobHandleForProducer(Dependency);
    }
}
