using System;

public struct PlayRequirement
{
    public int playId;
    public int subjectX, subjectY, subjectZ;
    public ValueRequirement cXValues;
    public ValueRequirement cYValues;
    public ValueRequirement cZValues;
    public FullRelationship relationshipX;
    public FullRelationship relationshipY;
    public TemplateMemory templateMemory;

    public bool TryAddFullRelationship(int id, FullRelationship tryAdd, FullRelationship template)
    {
        int tempSubjectX = 0, tempSubjectY = 0, tempSubjectZ = 0;
        bool test1 = false, test2 = false;

        if (template.subjectX == 1) if (subjectX == 0 || subjectX == tryAdd.subjectX) { tempSubjectX = tryAdd.subjectX; test1 = true; }
        if (template.subjectX == 2) if (subjectY == 0 || subjectY == tryAdd.subjectX) { tempSubjectY = tryAdd.subjectX; test1 = true; }
        if (template.subjectX == 3) if (subjectZ == 0 || subjectZ == tryAdd.subjectX) { tempSubjectZ = tryAdd.subjectX; test1 = true; }

        if (template.subjectY == 1) if (subjectX == 0 || subjectX == tryAdd.subjectY) { tempSubjectX = tryAdd.subjectY; test2 = true; }
        if (template.subjectY == 2) if (subjectY == 0 || subjectY == tryAdd.subjectY) { tempSubjectY = tryAdd.subjectY; test2 = true; }
        if (template.subjectY == 3) if (subjectZ == 0 || subjectZ == tryAdd.subjectY) { tempSubjectZ = tryAdd.subjectY; test2 = true; }

        if (test1 && test2)
        {
            subjectX = tempSubjectX;
            subjectY = tempSubjectY;
            subjectZ = tempSubjectZ;

            if (id == 1) relationshipX = tryAdd;
            if (id == 2) relationshipY = tryAdd;

            return true;
        }
        return false;
    }

    public bool CheckValuesInRange(int id, DataValues values, ValueRequirement template)
    {
        if (template.Equals(new ValueRequirement())) return true;

        if (id == 1) if (values.InRange(template)) return true;
        if (id == 2) if (values.InRange(template)) return true;
        if (id == 3) if (values.InRange(template)) return true;
        return false;
    }

    public bool CheckValidMemory(TemplateMemory memory, TemplateMemory template)
    {
        if (template.deed != memory.deed) return false;

        int tempSubjectX = 0, tempSubjectY = 0;
        bool test1 = false, test2 = false;

        if (template.subjectX == 1) if (memory.subjectX == subjectX) { tempSubjectX = subjectX; test1 = true; }
        if (template.subjectX == 2) if (memory.subjectX == subjectY) { tempSubjectX = subjectY; test1 = true; }
        if (template.subjectX == 3) if (memory.subjectX == subjectZ) { tempSubjectX = subjectZ; test1 = true; }

        if (template.subjectY == 1) if (memory.subjectY == subjectX) { tempSubjectY = subjectX; test2 = true; }
        if (template.subjectY == 2) if (memory.subjectY == subjectY) { tempSubjectY = subjectY; test2 = true; }
        if (template.subjectY == 3) if (memory.subjectY == subjectZ) { tempSubjectY = subjectZ; test2 = true; }
        
        if (test1 && test2)
        {
            templateMemory.subjectX = tempSubjectX;
            templateMemory.subjectY = tempSubjectY;
            templateMemory.deed = memory.deed;
            return true;
        }

        return false;
    }
}

public struct FullRelationship : IEquatable<FullRelationship>
{
    public int subjectX;
    public int subjectY;
    public TypeRelationship type;
    
    public bool Equals(FullRelationship o)
    {
        if (subjectX != o.subjectX && subjectX != o.subjectY) return false;
        if (subjectY != o.subjectX && subjectY != o.subjectY) return false;
        return true;
    }
}

public struct ValueRequirement
{
    public DataValues minValues;
    public DataValues maxValues;
}

public struct TemplateMemory
{
    public int subjectX;
    public int subjectY;
    public TypeDeed deed;
}

public class ExampleImp
{
    PlayRequirement p = new PlayRequirement()
    {
        subjectX = 1,
        subjectY = 2,
        subjectZ = 3,
        cXValues = new ValueRequirement()
        {
            minValues = new DataValues()
            {
                placeholder = .5f
            },
            maxValues = new DataValues()
            {
                placeholder = 1
            }
        },
        relationshipX = new FullRelationship()
        {
            subjectX = 1,
            subjectY = 3,
            type = TypeRelationship.Lover
        },
        templateMemory = new TemplateMemory()
        {
            subjectX = 2,
            subjectY = 3,
            deed = TypeDeed.Betrayed
        }
    };
}