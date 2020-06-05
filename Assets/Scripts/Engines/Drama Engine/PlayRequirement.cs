public struct PlayRequirement
{
    public int subjectX, subjectY, subjectZ;
    public DataValueRequirement cXValues;
    public DataValueRequirement cYValues;
    public DataValueRequirement cZValues;
    public FullRelationship relationshipX;
    public FullRelationship relationshipY;
    public TemplateMemory templateMemory;
}

public struct FullRelationship
{
    public int subjectX;
    public int subjectY;
    public TypeRelationship type;
}

public struct DataValueRequirement
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
        cXValues = new DataValueRequirement()
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