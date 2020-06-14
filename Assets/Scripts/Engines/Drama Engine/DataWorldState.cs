using Unity.Entities;
using Unity.Collections;
using System;

public struct StageId : IComponentData
{
    public int stageId;
}

public struct FullRelationship// : IEquatable<FullRelationship>
{
    public int subjectX;
    public int subjectY;
    public TypeRelationship type;

    public static bool Equals(FullRelationship test, FullRelationship target)
    {
        if (target.subjectX != test.subjectX && target.subjectX != test.subjectY) return false;
        if (target.subjectY != test.subjectX && target.subjectY != test.subjectY) return false;
        return true;
    }
}

public struct BufferFullRelationship : IBufferElementData
{
    public FullRelationship value;
}

public struct RunningPlay : IComponentData
{
    public DataRunningPlay runningPlay;
}

public struct DataRunningPlay
{
    public int stageId;
    public int playId;
    public int subjectX;
    public int subjectY;
    public int subjectZ;
}

public struct DataValues
{
    // define personality traits here
    public float placeholder;

    public static NativeArray<float> GetValues(Allocator a, DataValues data)
    {
        var valuesArray = new NativeArray<float>(G.valuesTraits, a);

        // Add each variable, in order, to the valuesArray
        valuesArray[0] = data.placeholder;

        return valuesArray;
    }

    public static void SetValues(NativeArray<float> v, DataValues data)
    {
        // Set each value here
        data.placeholder = v[0];
    }

    public static bool InRange(ValueRequirement template, DataValues data)
    {
        var test = true;
        if (data.placeholder < template.minValues.placeholder || data.placeholder > template.maxValues.placeholder) test = false;
        return test;
    }
}

public struct BufferDataValues : IBufferElementData
{
    public int factionId;
    public DataValues value;
}

public struct TemplateMemory
{
    public int subjectX;
    public int subjectY;
    public TypeDeed deed;
}

public struct BufferTemplateMemory : IBufferElementData
{
    public TemplateMemory value;
}

public struct ValueRequirement
{
    public DataValues minValues;
    public DataValues maxValues;
}