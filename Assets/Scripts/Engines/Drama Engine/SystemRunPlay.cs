using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class SystemRunPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    private NativeMultiHashMap<int, Line> PlayLibrary = new NativeMultiHashMap<int, Line>(G.numberOfPlays * G.linesPerPlay, Allocator.Persistent);
    private NativeHashMap<int, PlayRunner> PlayRunners;

    protected override void OnCreate()
    {
        EntityQuery query = GetEntityQuery(typeof(PlayRunner));
        var queryResult = query.ToComponentDataArray<PlayRunner>(Allocator.Persistent);
        PlayRunners = new NativeHashMap<int, PlayRunner>(G.numberOfStages, Allocator.Persistent);

        for (int i = 0; i < queryResult.Length; i++)
        {
            PlayRunners.Add(queryResult[i].stageId, queryResult[i]);
        }
        queryResult.Dispose();
    }

    protected override void OnDestroy()
    {

    }

    private EntityCommandBuffer.Concurrent Buffer;

    protected override void OnUpdate()
    {
        var buffer = Buffer;
        var playLibrary = PlayLibrary;
        var playRunners = PlayRunners;

        var bestPlays = new NativeHashMap<int, PotentialPlay>(G.numberOfStages, Allocator.Temp);

        // Build list of best plays per stage
        Entities.ForEach((Entity entity, Situation situation, DynamicBuffer<PotentialPlay> validPlays) =>
        {
            for (int i = 0; i < validPlays.Length; i++)
            {
                if (!bestPlays.ContainsKey(situation.stageId))
                {
                    bestPlays.Add(situation.stageId, validPlays[i]);
                }
                else
                {
                    if (validPlays[i].drama > bestPlays[situation.stageId].drama)
                    {
                        bestPlays.Add(situation.stageId, validPlays[i]);
                    }
                }
            }
        })
        .WithBurst()
        .Schedule();

        // Start a play on a stage 
        Entities.WithNone<Line>().
        ForEach((Entity entity, int entityInQueryIndex, PlayRunner playRunner) =>
        {
            var newPlayRunner = new PlayRunner
            {
                playId = bestPlays[playRunner.stageId].playId,
                stageId = playRunner.stageId
            };

            buffer.SetComponent(entityInQueryIndex, entity, newPlayRunner);
        })
        .WithBurst()
        .Schedule();

        var time = Time.DeltaTime;

        Entities.ForEach((Entity entity, int entityInQueryIndex, PlayRunner playRunner) =>
        {
            // increment play line time
            var dif = time - playRunner.timeLineUpdated;
            playRunner.lineTime = playRunner.lineTime + dif;
            playRunner.timeLineUpdated = time;

            // generate step request if time is up for running line
            if (playRunner.lineTime > playRunner.lineTimeMax)
            {
                var p = new PlayLineRequest
                {
                    newLine = 0
                };
                buffer.AddComponent(entityInQueryIndex, entity, p);
            }
        })
        .WithBurst()
        .Schedule();

        Entities.ForEach((Entity entity, int entityInQueryIndex, PlayRunner playRunner, Line playingLine, PlayLineRequest newLine, PlayActorIds actors) =>
        {
            var newPlayRunner = new PlayRunner();
            if (newLine.newLine == 0) { newPlayRunner.lineId = playingLine.childA; }
            if (newLine.newLine == 1) { newPlayRunner.lineId = playingLine.childB; }
            if (newLine.newLine == 2) { newPlayRunner.lineId = playingLine.childC; }
            if (newLine.newLine == 3) { newPlayRunner.lineId = playingLine.childD; }

            // process play step requests
            var playLines = playLibrary.GetValuesForKey(playRunner.playId);

            var nextLine = new Line();
            int i = 0;
            while (playLines.MoveNext())
            {
                i++;
                nextLine = playLines.Current;
                if (newPlayRunner.lineId == i) { break; }
            }

            newPlayRunner.lineTime = 0;
            newPlayRunner.lineTimeMax = nextLine.life;
            newPlayRunner.playId = playRunner.playId;
            newPlayRunner.stageId = playRunner.stageId;
            newPlayRunner.timeLineUpdated = time;

            buffer.SetComponent(entityInQueryIndex, entity, newPlayRunner);
            buffer.SetComponent(entityInQueryIndex, entity, nextLine);

            // release stage for new play
            if (nextLine.isEnd)
            {
                buffer.RemoveComponent<Line>(entityInQueryIndex, entity);
            }
            // Send dialogue request to dialogue system
            else
            {
                var e = buffer.CreateEntity(entityInQueryIndex);

                var speaker = 0;
                if (nextLine.speaker == 0) { speaker = actors.alpha; }
                if (nextLine.speaker == 1) { speaker = actors.beta; }
                if (nextLine.speaker == 2) { speaker = actors.gamma; }

                var dr = new DialogueRequest
                {
                    actorId = nextLine.speaker,
                    dialogueId = nextLine.dialogueId
                };

                buffer.AddComponent(entityInQueryIndex, e, dr);
            }
        })
        .WithBurst()
        .Schedule();
    }
}

public struct PlayRunner : IComponentData
{
    public int stageId;
    public int playId;
    public int lineId;
    public float timeLineUpdated;
    public float lineTime;
    public float lineTimeMax;
}

public struct Line : IComponentData
{
    public int dialogueId;
    public int speaker;
    public int childA;
    public int childB;
    public int childC;
    public int childD;
    public bool isEnd;
    public float life;
}

public struct PlayLineRequest : IComponentData
{
    public int newLine;
}

public struct DialogueRequest : IComponentData
{
    public int actorId;
    public int dialogueId;
}