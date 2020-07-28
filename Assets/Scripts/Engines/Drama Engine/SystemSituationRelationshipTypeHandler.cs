using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class SystemSituationRelationshipTypeHandler : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    protected override void OnCreate()
    {
        Buffer = ESECBS.CreateCommandBuffer().ToConcurrent();
    }

    protected override void OnDestroy()
    {
        
    }

    private EntityCommandBuffer.Concurrent Buffer;

    protected override void OnUpdate()
    {
        var buffer = Buffer;
        var stageIds = new NativeList<int>(Allocator.Temp);

        // Get stage ids
        Entities.ForEach((Entity entity, PartialSituation situation, NeedsRelationshipType need) =>
        {
            if (!stageIds.Contains(situation.stageId))
            {
                stageIds.Add(situation.stageId);
            }
        })
        .WithBurst()
        .Run();

        var occupantsHashmap = new NativeMultiHashMap<int, int>(G.maxNPCPopulation / 10, Allocator.Temp);

        // Get actors from stage
        Entities.ForEach((Entity entity, StageId stageId, DynamicBuffer<Occupant> occupants) => 
        {
            if (stageIds.Contains(stageId.value))
            {
                for (int i = 0; i < occupants.Length; i++)
                {
                    occupantsHashmap.Add(stageId.value, occupants[i].id);
                }
            }
        })
        .WithBurst()
        .Run();

        var relationshipsPerStage = new NativeMultiHashMap<int, ActorRelationship>(G.maxRelationships / 10, Allocator.Temp);

        // Get relationships from actors
        Entities.ForEach((Entity entity, int entityInQueryIndex, StageOccupant stageOccupant, DynamicBuffer<ActorRelationship> relationships) =>
        {
            if (occupantsHashmap.ContainsKey(stageOccupant.stageId))
            {
                for (int i = 0; i < relationships.Length; i++)
                {
                    relationshipsPerStage.Add(stageOccupant.stageId, relationships[i]);
                }
            }
        })
        .WithBurst()
        .Run();

        // Generate various child entities from initial situation. Success!
        Entities.ForEach((Entity entity, int entityInQueryIndex, PartialSituation situation, NeedsRelationshipType need, DynamicBuffer<StageParameters> parameters) =>
        {
            var occupants = occupantsHashmap.GetValuesForKey(situation.stageId);
            var relationships = relationshipsPerStage.GetValuesForKey(situation.stageId);
            var occupantsList = new NativeList<int>(Allocator.TempJob);

            int x = 0;
            while (occupants.MoveNext())
            {
                occupantsList[x] = occupants.Current;
                x = x + 1;
            }

            var numberOfOccupants = occupantsList.Length;

            for (int i = 0; i < numberOfOccupants - 2; i++)
            {
                for (int j = 0; j < numberOfOccupants - 1; j++)
                {
                    for (int k = 0; k < numberOfOccupants; k++)
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
            
            occupantsList.Dispose();
        })
        .WithBurst()
        .Schedule();

        stageIds.Dispose();
        occupantsHashmap.Dispose();
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