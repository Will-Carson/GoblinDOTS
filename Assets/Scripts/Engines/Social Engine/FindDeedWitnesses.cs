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
        var ecb = ESECBS.CreateCommandBuffer().AsParallelWriter();

        Entities
        .ForEach((
            int entityInQueryIndex,
            ref DynamicBuffer<FindDeedWitnessesRequest> requests,
            in Entity entity,
            in DynamicBuffer<Occupant> occupants) =>
        {
            for (int i = 0; i < requests.Length; i++)
            {
                for (int j = 0; j < occupants.Length; j++)
                {
                    var witnessedEvent = new WitnessedEvent
                    {
                        deedDoerFMId = requests[i].deedDoerFactionMemberId,
                        deedTargetFMId = requests[i].deedTargetFactionMemberId,
                        deedWitnessFactionMemberId = occupants[j].id,
                        isRumor = false,
                        needsEvaluation = true,
                        reliability = 1,
                        rumorSpreaderFMId = 0,
                        type = requests[i].type
                    };
                    ecb.AppendToBuffer(entityInQueryIndex, occupants[j].occupant, witnessedEvent);
                }
                requests.RemoveAt(i);
            }
        })
        .WithBurst()
        .Schedule();

        ESECBS.AddJobHandleForProducer(Dependency);
    }
}
