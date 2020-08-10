using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class FinishPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    public NativeMultiHashMap<int, PlayConsiquence> PlayEndings 
        = new NativeMultiHashMap<int, PlayConsiquence>(G.maxValidPlays, Allocator.Persistent);
    
    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().ToConcurrent();
        var playEndings = PlayEndings;

        Entities
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            PlayRunner playRunner,
            Line playingLine) =>
        {
            // Release stages for new plays
            if (playingLine.isEnd)
            {
                ecb.SetComponent(entityInQueryIndex, entity, new PlayRunner { stageId = playRunner.stageId });
                ecb.RemoveComponent<Line>(entityInQueryIndex, entity);
                ecb.AddComponent<NeedsPlay>(entityInQueryIndex, entity);

                NativeMultiHashMapIterator<int>? it = default;
                while (playEndings.TryIterate(playingLine.endingId, out PlayConsiquence consiquence, ref it))
                {
                    ecb.AppendToBuffer(entityInQueryIndex, entity, consiquence);
                }
                return;
            }
        })
        .WithBurst()
        .Schedule();

        Entities
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            PlayActorIds actors,
            DynamicBuffer<PlayConsiquence> consiquences) =>
        {
            for (int i = 0; i < consiquences.Length; i++)
            {
                var ending = consiquences[i];
                switch (ending.type)
                {
                    case Consiquence.Build:
                        break;
                    case Consiquence.Gift:
                        break;
                    case Consiquence.Peace:
                        break;
                    case Consiquence.Siege:
                        break;
                    case Consiquence.Steal:
                        var deedDoer = 0;
                        var deedReciever = 0;
                        if (ending.value1 == 0) deedDoer = actors.alpha;
                        if (ending.value1 == 1) deedDoer = actors.beta;
                        if (ending.value1 == 2) deedDoer = actors.gamma;
                        
                        if (ending.value2 == 0) deedReciever = actors.alpha;
                        if (ending.value2 == 1) deedReciever = actors.beta;
                        if (ending.value2 == 2) deedReciever = actors.gamma;

                        var deed = new WitnessedEvent
                        {
                            deedDoerFactionMemberId = deedDoer,
                            deedTargetFactionMemberId = deedReciever,
                            deedWitnessFactionMemberId = 0,
                            isRumor = false,
                            needsEvaluation = true,
                            reliability = 1,
                            rumorSpreaderFactionMemberId = 0,
                            type = DeedType.Robbed
                        };
                        break;
                    case Consiquence.War:
                        break;
                }
            }
            ecb.SetBuffer<PlayConsiquence>(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);
    }
}

public struct PlayConsiquence : IBufferElementData
{
    public Consiquence type;
    public int value1;
    public int value2;
    public int value3;
    public int value4;
}

public enum Consiquence
{
    Relationship,
    Build,
    Siege,
    War,
    Peace,
    Steal,
    Gift
}

public struct FindDeedWitnesses : IBufferElementData
{
    // TODO Build system to find deeds
}