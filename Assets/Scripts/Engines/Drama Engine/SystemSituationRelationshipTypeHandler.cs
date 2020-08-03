using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class SystemSituationRelationshipTypeHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    protected override void OnDestroy()
    {
        
    }

    private EntityCommandBuffer.Concurrent Buffer;

    protected override void OnUpdate()
    {
        var buffer = ESECBS.CreateCommandBuffer().ToConcurrent();
        var stageIds = new NativeList<int>(Allocator.TempJob);

        // Get stage ids
        Entities.ForEach((Entity entity, PartialSituation situation, NeedsRelationshipType need) =>
        {
            if (!stageIds.Contains(situation.stageId))
            {
                stageIds.Add(situation.stageId);
            }
        })
        .WithBurst()
        .Schedule();

        var occupantsByStage = new NativeMultiHashMap<int, int>(G.maxNPCPopulation / 10, Allocator.TempJob);

        // Get actors from stage
        Entities.ForEach((Entity entity, StageId stageId, DynamicBuffer<Occupant> occupants) => 
        {
            if (stageIds.Contains(stageId.value))
            {
                for (int i = 0; i < occupants.Length; i++)
                {
                    occupantsByStage.Add(stageId.value, occupants[i].id);
                }
            }
        })
        .WithBurst()
        .Schedule();

        var relationshipsPerStage = new NativeMultiHashMap<int, ActorRelationship>(G.maxRelationships / 10, Allocator.TempJob);

        // Get relationships from actors
        Entities.ForEach((Entity entity, int entityInQueryIndex, StageOccupant stageOccupant, DynamicBuffer<ActorRelationship> relationships) =>
        {
            if (occupantsByStage.ContainsKey(stageOccupant.stageId))
            {
                for (int i = 0; i < relationships.Length; i++)
                {
                    relationshipsPerStage.Add(stageOccupant.stageId, relationships[i]);
                }
            }
        })
        .WithBurst()
        .Schedule();

        var occupantsList = new NativeList<int>(Allocator.Persistent);

        // Generate various child entities from initial situation. Success!
        Entities.ForEach((Entity entity, int entityInQueryIndex, PartialSituation situation, NeedsRelationshipType need, DynamicBuffer<StageParameters> parameters) =>
        {
            var occupants = occupantsByStage.GetValuesForKey(situation.stageId);
            var relationships = relationshipsPerStage.GetValuesForKey(situation.stageId);
            
            while (occupants.MoveNext()) occupantsList.Add(occupants.Current);

            var numberOfOccupants = occupantsList.Length;

            for (int i = 0; i < numberOfOccupants - 2; i++)
            {
                for (int j = 1; j < numberOfOccupants - 1; j++)
                {
                    for (int k = 2; k < numberOfOccupants; k++)
                    {
                        var roles = new PlayActorIds
                        {
                            alpha = occupantsList[i],
                            beta = occupantsList[j],
                            gamma = occupantsList[k]
                        };
                        
                        var e = buffer.Instantiate(entityInQueryIndex, entity);
                        buffer.AddComponent(entityInQueryIndex, e, roles);
                        while (relationships.MoveNext())
                        {
                            var r = relationships.Current;
                            if (r.owner == roles.alpha && r.target == roles.beta)
                            {
                                var s = new StageParameters
                                {
                                    param = new Parameter
                                    {
                                        op = Operator.Equal,
                                        type = ParameterType.RelationshipType,
                                        value1 = roles.alpha,
                                        value2 = r.type,
                                        value3 = roles.beta
                                    }
                                };
                                buffer.AppendToBuffer(entityInQueryIndex, entity, s);
                            }
                            if (r.owner == roles.alpha && r.target == roles.gamma)
                            {
                                var s = new StageParameters
                                {
                                    param = new Parameter
                                    {
                                        op = Operator.Equal,
                                        type = ParameterType.RelationshipType,
                                        value1 = roles.alpha,
                                        value2 = r.type,
                                        value3 = roles.gamma
                                    }
                                };
                                buffer.AppendToBuffer(entityInQueryIndex, entity, s);
                            }
                            if (r.owner == roles.beta && r.target == roles.alpha)
                            {
                                var s = new StageParameters
                                {
                                    param = new Parameter
                                    {
                                        op = Operator.Equal,
                                        type = ParameterType.RelationshipType,
                                        value1 = roles.beta,
                                        value2 = r.type,
                                        value3 = roles.alpha
                                    }
                                };
                                buffer.AppendToBuffer(entityInQueryIndex, entity, s);
                            }
                            if (r.owner == roles.beta && r.target == roles.gamma)
                            {
                                var s = new StageParameters
                                {
                                    param = new Parameter
                                    {
                                        op = Operator.Equal,
                                        type = ParameterType.RelationshipType,
                                        value1 = roles.beta,
                                        value2 = r.type,
                                        value3 = roles.gamma
                                    }
                                };
                                buffer.AppendToBuffer(entityInQueryIndex, entity, s);
                            }
                            if (r.owner == roles.gamma && r.target == roles.alpha)
                            {
                                var s = new StageParameters
                                {
                                    param = new Parameter
                                    {
                                        op = Operator.Equal,
                                        type = ParameterType.RelationshipType,
                                        value1 = roles.gamma,
                                        value2 = r.type,
                                        value3 = roles.alpha
                                    }
                                };
                                buffer.AppendToBuffer(entityInQueryIndex, entity, s);
                            }
                            if (r.owner == roles.gamma && r.target == roles.beta)
                            {
                                var s = new StageParameters
                                {
                                    param = new Parameter
                                    {
                                        op = Operator.Equal,
                                        type = ParameterType.RelationshipType,
                                        value1 = roles.gamma,
                                        value2 = r.type,
                                        value3 = roles.beta
                                    }
                                };
                                buffer.AppendToBuffer(entityInQueryIndex, entity, s);
                            }
                        }
                    }
                }
            }
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        Debug.Log(relationshipsPerStage.CountValuesForKey(0));

        ESECBS.AddJobHandleForProducer(Dependency);

        Dependency.Complete();
        stageIds.Dispose();
        occupantsList.Dispose();
        occupantsByStage.Dispose();
        relationshipsPerStage.Dispose();
    }
}

public struct ActorId : IComponentData
{
    public int value;
}

public struct ActorRelationship : IBufferElementData
{
    public int owner;
    public int type;
    public int target;
}

public struct StageOccupant : IComponentData
{
    public int stageId;
}

public struct PlayActorIds : IComponentData
{
    public int alpha;
    public int beta;
    public int gamma;
}