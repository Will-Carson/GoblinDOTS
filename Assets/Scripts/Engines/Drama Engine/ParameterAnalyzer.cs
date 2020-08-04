using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class ParameterAnalyzer : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    public NativeMultiHashMap<int, Parameter> PlaysRequirements = new NativeMultiHashMap<int, Parameter>(G.numberOfPlays, Allocator.Persistent);
    public NativeHashMap<int, int> PlayDramaValues = new NativeHashMap<int, int>(G.numberOfPlays, Allocator.Persistent);

    protected override void OnDestroy()
    {
        PlaysRequirements.Dispose();
        PlayDramaValues.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = ESECBS.CreateCommandBuffer().ToConcurrent();
        var playRequirments = PlaysRequirements;
        var playDramaValues = PlayDramaValues;
        var ps = new NativeList<SituationParameters>(Allocator.TempJob);

        Entities
        .ForEach((Entity entity, int entityInQueryIndex, Situation situation, DynamicBuffer<SituationParameters> situationParameters, DynamicBuffer<PotentialPlay> validPlays) =>
        {
            for (int j = 0; j < situationParameters.Length; j++)
            {
                ps.Add(situationParameters[j]);
            }

            for (int j = 0; j < playRequirments.Count(); j++)
            {
                var playParameters = playRequirments.GetValuesForKey(j);
                var playIsValid = true;

                while (playParameters.MoveNext())
                {
                    var parameterMet = false;
                    var p = playParameters.Current;
                    switch (p.op)
                    {
                        case Operator.Equal:
                            for (int i = 0; i < situationParameters.Length; i++)
                            {
                                if (p.type != situationParameters[i].param.type) break;
                                if (Same(situationParameters[i].param, p))
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.NotEqual:
                            for (int i = 0; i < situationParameters.Length; i++)
                            {
                                if (p.type != situationParameters[i].param.type) break;
                                if (!Same(situationParameters[i].param, p))
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.GreaterThan:
                            for (int i = 0; i < situationParameters.Length; i++)
                            {
                                if (p.type != situationParameters[i].param.type) break;
                                if (situationParameters[i].param.value1 > p.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.LessThan:
                            for (int i = 0; i < situationParameters.Length; i++)
                            {
                                if (p.type != situationParameters[i].param.type) break;
                                if (situationParameters[i].param.value1 < p.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.GreaterOrEqual:
                            for (int i = 0; i < situationParameters.Length; i++)
                            {
                                if (p.type != situationParameters[i].param.type) break;
                                if (situationParameters[i].param.value1 >= p.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.LessOrEqual:
                            for (int i = 0; i < situationParameters.Length; i++)
                            {
                                if (p.type != situationParameters[i].param.type) break;
                                if (situationParameters[i].param.value1 <= p.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.Between:
                            for (int i = 0; i < situationParameters.Length; i++)
                            {
                                if (p.type != situationParameters[i].param.type) break;
                                if (p.value1 < situationParameters[i].param.value1 && p.value2 > situationParameters[i].param.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                    }

                    if (!parameterMet)
                    {
                        playIsValid = false;
                        break;
                    }
                }

                if (playIsValid)
                {
                    var pid = new PotentialPlay();
                    pid.playId = j;
                    // TODO
                    pid.drama = playDramaValues[j];
                    ecb.AppendToBuffer(entityInQueryIndex, entity, pid);
                    ecb.RemoveComponent<SituationParameters>(entityInQueryIndex, entity);
                }
            }
        })
        .WithBurst()
        .Schedule();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);
        ps.Dispose();
    }
    
    public static bool Same(Parameter s1, Parameter s2)
    {
        if (s1.op != s2.op) return false;
        if (s1.type != s2.type) return false;
        if (s1.value1 != s2.value1) return false;
        if (s1.value2 != s2.value2) return false;
        if (s1.value3 != s2.value3) return false;
        return true;
    }
}

public enum Operator
{
    Equal = default,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    Between
}

public enum ParameterType
{
    NumberOfActors,
    RelationshipType
}

public struct Situation : IComponentData { public int stageId; }

public struct Parameter
{
    public ParameterType type;
    public Operator op;
    public int value1;
    public int value2;
    public int value3;
}

public struct SituationParameters : IBufferElementData
{
    public Parameter param;
}

public struct PotentialPlay : IBufferElementData { public int playId; public int drama; }

public struct PlayRequirement { public Parameter param; }

