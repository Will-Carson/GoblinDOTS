using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class FinishPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    public NativeList<PlayEnding> PlayEndings 
        = new NativeList<PlayEnding>(Allocator.Persistent);

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
                ecb.AddComponent(entityInQueryIndex, entity, playEndings[playingLine.endingId]);
                return;
            }
        })
        .WithBurst()
        .Schedule();

        Entities
        .ForEach((
            Entity entity,
            int entityInQueryIndex,
            PlayEnding ending) =>
        {
            if (ending.type == Consiquence.Build)
            {

            }
            if (ending.type == Consiquence.Gift)
            {

            }
            if (ending.type == Consiquence.Peace)
            {

            }
            if (ending.type == Consiquence.Relationship)
            {

            }
            if (ending.type == Consiquence.Siege)
            {

            }
            if (ending.type == Consiquence.Steal)
            {

            }
            if (ending.type == Consiquence.War)
            {

            }
            ecb.RemoveComponent<PlayEnding>(entityInQueryIndex, entity);
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);
    }
}

public struct PlayEnding : IComponentData
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