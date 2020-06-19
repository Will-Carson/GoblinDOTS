using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

// TODO PEL has no place here. This system isn't actually running the plays, so the PEL doesn't matter.
[ServerWorld]
public class SystemRunPlay : SystemBase
{
    // NativeMultiMap so we can run most of it with Burst enabled
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    
    public NativeMultiHashMap<int, LineServerData> PEL 
        = new NativeMultiHashMap<int, LineServerData>(G.numberOfPlays, Allocator.Persistent);
    public NativeHashMap<Entity, SystemDataRunningPlay> RunningPlays 
        = new NativeHashMap<Entity, SystemDataRunningPlay>(G.numberOfStages, Allocator.Persistent);
    
    protected override void OnDestroy()
    {
        PEL.Dispose();
        RunningPlays.Dispose();
    }

    protected override void OnCreate()
    {
        PEL.Add(0, new LineServerData());
    }

    protected override void OnUpdate()
    {
        var trackedEntities = RunningPlays;
        var buffer = ESECBS.CreateCommandBuffer();
        var time = Time.DeltaTime;
        var X = new SystemDataRunningPlay();

        #region Track running play objects with system 
        Entities
            .WithNone<SystemDataRunningPlay>()
            .ForEach((Entity entity, in PlayDataComponent runningPlay, in StageId stageId) =>
            {
                var s = new SystemDataRunningPlay() { /*set values*/ };
                buffer.AddComponent<SystemDataRunningPlay>(entity);
                buffer.SetComponent(entity, s);
                trackedEntities[entity] = s;
            })
            .WithBurst()
            .Schedule();

        Entities
            .WithNone<PlayDataComponent>()
            .ForEach((Entity entity, in SystemDataRunningPlay system, in StageId stageId) =>
            {
                buffer.RemoveComponent<SystemDataRunningPlay>(entity);
                trackedEntities[entity] = X;
            })
            .WithBurst()
            .Schedule();
        #endregion

        // Update plays as necessary
        // TODO burstify me
        var entities = RunningPlays.GetKeyArray(Allocator.Temp);
        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            var runningPlay = RunningPlays[entity];
            var runningPlayData = runningPlay.runningPlay;
            var playLibrary = PEL.GetValuesForKey(runningPlay.runningPlay.playId);
            var currentLine = new LineServerData();
            
            foreach (var line in playLibrary)
            {
                if (line.id == runningPlayData.currentLineId)
                {
                    currentLine = line;
                    break;
                }
            }

            // If the running play is ready for the next line, create a continue play request
            if (runningPlayData.lastUpdated + currentLine.life <= time)
            {
                var updateRequest = new UpdatePlayRequest()
                {
                    data = new DataRunningPlay()
                    {
                        stageId = runningPlayData.stageId,
                        playId = runningPlayData.playId,
                        currentLineId = currentLine.childLineAId,
                        lastLineId = runningPlayData.currentLineId,
                        lastUpdated = time,
                        subjectX = runningPlayData.subjectX,
                        subjectY = runningPlayData.subjectY,
                        subjectZ = runningPlayData.subjectZ,
                    }
                };
                buffer.AddComponent(buffer.CreateEntity(), updateRequest);
            }
        }
    }
}

public struct SystemDataRunningPlay : ISystemStateComponentData
{
    public DataRunningPlay runningPlay;
}