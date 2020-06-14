using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

[ServerWorld]
[UpdateAfter(typeof(TransformSystemGroup))]
public class SystemFindValidPlay : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    private NativeArray<PlayRequirement> PlayRequirementsLibrary = new NativeArray<PlayRequirement>(G.numberOfPlays, Allocator.Persistent);
    private EntityQueryDesc QueryDesc;

    protected override void OnCreate()
    {
        QueryDesc = new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<StageId>(),
                ComponentType.ReadOnly<BufferFullRelationship>(),
                ComponentType.ReadOnly<BufferTemplateMemory>(),
                ComponentType.ReadOnly<BufferDataValues>()
            },
            None = new ComponentType[]
            {
                typeof(RunningPlay)
            }
        };
        PlayRequirementsLibrary[0] = new PlayRequirement();
    }
    
    [BurstCompile]
    public struct FindValidPlays : IJobChunk
    {
        [ReadOnly] public NativeArray<PlayRequirement> playRequirementsLibrary;
        [ReadOnly] public ArchetypeChunkEntityType entityArchetype;
        [ReadOnly] public ArchetypeChunkComponentType<StageId> stageIdArchetype;
        [ReadOnly] public ArchetypeChunkBufferType<BufferFullRelationship> fullRelationshipArchetype;
        [ReadOnly] public ArchetypeChunkBufferType<BufferTemplateMemory> templateMemoryArchetype;
        [ReadOnly] public ArchetypeChunkBufferType<BufferDataValues> valuesComponentArchetype;
        [ReadOnly] public EntityCommandBuffer.Concurrent buffer;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var stageIds = chunk.GetNativeArray(stageIdArchetype);
            var fullRelationshipBuffers = chunk.GetBufferAccessor(fullRelationshipArchetype);
            var memoryTemplateBuffers = chunk.GetBufferAccessor(templateMemoryArchetype);
            var valuesComponentBuffers = chunk.GetBufferAccessor(valuesComponentArchetype);
            var chunkEntities = chunk.GetNativeArray(entityArchetype);

            for (int i = 0; i < chunk.Count; i++)
            {
                var runningPlay = new RunningPlay();

                var stageData = new StageData()
                {
                    fullRelationships = fullRelationshipBuffers[i],
                    templateMemories = memoryTemplateBuffers[i],
                    valuesComponents = valuesComponentBuffers[i]
                };

                var validPlay = new RunningPlay();

                for(int j = 0; j < playRequirementsLibrary.Length; j++)
                {
                    if (Requirements(playRequirementsLibrary[j], out runningPlay, stageData))
                    {
                        validPlay = runningPlay;
                    }
                }

                buffer.SetComponent(chunkIndex, chunkEntities[i], validPlay);
            }
        }
    }

    private static bool Requirements(
            PlayRequirement playRequirement,
            out RunningPlay runningPlay,
            StageData stageData)
    {
        var fullRelationships = stageData.fullRelationships.ToNativeArray(Allocator.TempJob);
        var valueComponents = stageData.valuesComponents;
        var templateMemories = stageData.templateMemories;

        runningPlay = new RunningPlay()
        {
            runningPlay = new DataRunningPlay()
            {
                playId = playRequirement.playId,
                stageId = stageData.stageId.stageId
            }
        };
        var validPlays = new NativeList<PlayRequirement>(10, Allocator.TempJob);

        // Populate list with items meeting one of the constraints
        for (var i = 0; i < fullRelationships.Length; i++)
        {
            var validPlay = new PlayRequirement();
            PlayRequirement.TryAddFullRelationship(1, fullRelationships[i].value, playRequirement.relationshipX, validPlay, out validPlay);
            validPlays.Add(validPlay);
        }

        // Check if there are any items that meet the constraint. If not, this play is invalid.
        if (validPlays.Length == 0) return false;

        // Remove items from the list that do not meet another constraint
        for (var i = validPlays.Length - 1; i >= 0; i--)
        {
            for (var j = 0; j < fullRelationships.Length; j++)
            {
                var newValidPlay = new PlayRequirement();
                if (!PlayRequirement.TryAddFullRelationship(2, fullRelationships[j].value, playRequirement.relationshipY, validPlays[i], out newValidPlay))
                    validPlays[i] = newValidPlay;
                else validPlays.RemoveAt(i);
            }
        }

        // Every time we get done removing items we check again
        if (validPlays.Length == 0) return false;

        // Repeat...
        var factionValues = new NativeHashMap<int, DataValues>(valueComponents.Length, Allocator.Persistent);
        for (var i = 0; i < valueComponents.Length; i++)
        {
            factionValues.Add(valueComponents[i].factionId, valueComponents[i].value);
        }

        for (var i = validPlays.Length - 1; i >= 0; i--)
        {
            var test = true;
            if (PlayRequirement.CheckValuesInRange(factionValues[validPlays[i].subjectX], playRequirement.cXValues)) test = false;
            if (PlayRequirement.CheckValuesInRange(factionValues[validPlays[i].subjectY], playRequirement.cYValues)) test = false;
            if (PlayRequirement.CheckValuesInRange(factionValues[validPlays[i].subjectZ], playRequirement.cZValues)) test = false;
            if (!test) validPlays.RemoveAt(i);
        }

        if (validPlays.Length == 0) return false;

        for (var i = validPlays.Length - 1; i >= 0; i--)
        {
            var test = false;
            for (var j = 0; j < templateMemories.Length; j++)
            {
                var newValidPlay = new PlayRequirement();
                if (PlayRequirement.CheckValidMemory(templateMemories[j].value, playRequirement.templateMemory, validPlays[i], out newValidPlay))
                {
                    test = true;
                    validPlays[i] = newValidPlay;
                }
            }
            if (!test) validPlays.RemoveAt(i);
        }

        if (validPlays.Length == 0) return false;

        // Set remaining play request data before returning.
        runningPlay.runningPlay.subjectX = validPlays[0].subjectX;
        runningPlay.runningPlay.subjectY = validPlays[0].subjectY;
        runningPlay.runningPlay.subjectZ = validPlays[0].subjectZ;

        // If all constraints are met and we have any items left over, return true.
        return true;
    }

    public struct StageData
    {
        public StageId stageId;
        public DynamicBuffer<BufferFullRelationship> fullRelationships;
        public DynamicBuffer<BufferTemplateMemory> templateMemories;
        public DynamicBuffer<BufferDataValues> valuesComponents;
    }

    EntityCommandBuffer.Concurrent commandBuffer;
    protected override void OnUpdate()
    {
        commandBuffer = ESECBS.CreateCommandBuffer().ToConcurrent();

        var job = new FindValidPlays();

        job.playRequirementsLibrary = PlayRequirementsLibrary;
        job.entityArchetype = GetArchetypeChunkEntityType();
        job.stageIdArchetype = GetArchetypeChunkComponentType<StageId>(true);
        job.fullRelationshipArchetype = GetArchetypeChunkBufferType<BufferFullRelationship>(true);
        job.templateMemoryArchetype = GetArchetypeChunkBufferType<BufferTemplateMemory>(true);
        job.valuesComponentArchetype = GetArchetypeChunkBufferType<BufferDataValues>(true);
        job.buffer = commandBuffer;

        Dependency = job.Schedule(GetEntityQuery(QueryDesc), Dependency);
        Dependency.Complete();

        ESECBS.AddJobHandleForProducer(Dependency);
    }

    protected override void OnDestroy()
    {
        Dependency.Complete();
        PlayRequirementsLibrary.Dispose();
    }
}