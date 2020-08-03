using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using UnityEngine;

[ServerWorld]
public class SystemParameterAnalyzer : SystemBase
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    public NativeMultiHashMap<int, Parameter> Plays = new NativeMultiHashMap<int, Parameter>(G.numberOfPlays, Allocator.Persistent);
    public NativeHashMap<int, int> PlayDramaValues = new NativeHashMap<int, int>(G.numberOfPlays, Allocator.Persistent);

    protected override void OnDestroy()
    {
        Plays.Dispose();
        PlayDramaValues.Dispose();
    }

    protected override void OnUpdate()
    {
        var plays = Plays;
        var ps = new NativeList<StageParameters>(Allocator.TempJob);

        Entities.ForEach((Entity entity, Situation situation, DynamicBuffer<StageParameters> parameters, DynamicBuffer<PotentialPlay> validPlays) =>
        {
            for (int j = 0; j < parameters.Length; j++)
            {
                ps.Add(parameters[j]);
            }

            for (int j = 0; j < plays.Count(); j++)
            {
                var play = plays.GetValuesForKey(j);
                var playIsValid = true;

                while (play.MoveNext())
                {
                    var parameterMet = false;
                    switch (play.Current.op)
                    {
                        case Operator.Equal:
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (Same(parameters[i].param, play.Current))
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.NotEqual:
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (!Same(parameters[i].param, play.Current))
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.GreaterThan:
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (play.Current.value1 > parameters[i].param.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.LessThan:
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (play.Current.value1 < parameters[i].param.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.GreaterOrEqual:
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (play.Current.value1 >= parameters[i].param.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.LessOrEqual:
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (play.Current.value1 <= parameters[i].param.value1)
                                {
                                    parameterMet = true;
                                    break;
                                }
                            }
                            break;
                        case Operator.Between:
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (play.Current.value1 < parameters[i].param.value1 && play.Current.value2 > parameters[i].param.value1)
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
                    pid.drama = 0;
                    validPlays.Add(pid);
                }
            }
        })
        .WithoutBurst()
        .Run();

        Dependency.Complete();

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

public struct StageParameters : IBufferElementData
{
    public Parameter param;
}

public struct PotentialPlay : IBufferElementData { public int playId; public int drama; }

public struct PlayRequirement { public Parameter param; }

