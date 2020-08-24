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
        var ecb = ESECBS.CreateCommandBuffer().AsParallelWriter();
        var playEndings = PlayEndings;

        Entities
        .ForEach((
            int entityInQueryIndex,
            in Entity entity,
            in PlayRunner playRunner,
            in Line playingLine) =>
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
            int entityInQueryIndex,
            ref DynamicBuffer<PlayConsiquence> consiquences,
            in Entity entity,
            in PlayActorIds actors) =>
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
                    #region Robbed
                    case Consiquence.Robbed:
                        var deedDoer = 0;
                        var deedReciever = 0;
                        if (ending.value1 == 0) deedDoer = actors.alpha;
                        if (ending.value1 == 1) deedDoer = actors.beta;
                        if (ending.value1 == 2) deedDoer = actors.gamma;
                        
                        if (ending.value2 == 0) deedReciever = actors.alpha;
                        if (ending.value2 == 1) deedReciever = actors.beta;
                        if (ending.value2 == 2) deedReciever = actors.gamma;

                        var deed = new FindDeedWitnessesRequest
                        {
                            deedDoerFactionMemberId = deedDoer,
                            deedTargetFactionMemberId = deedReciever,
                            type = DeedType.Robbed
                        };

                        ecb.AppendToBuffer(entityInQueryIndex, entity, deed);
                        break;
                    #endregion
                    case Consiquence.War:
                        break;                    
                }
                consiquences.RemoveAt(i);
            }
        })
        .WithBurst()
        .Schedule();
        
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
    Build,
    Siege,
    War,
    Peace,
    Robbed,
    Gift
}

public struct FindDeedWitnessesRequest : IBufferElementData
{
    public int deedDoerFactionMemberId;
    public int deedTargetFactionMemberId;
    public DeedType type;
}