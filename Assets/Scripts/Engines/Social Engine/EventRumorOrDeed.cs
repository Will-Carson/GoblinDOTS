using Unity.Entities;

public struct EventWitness : IBufferElementData
{
    public bool isRumor;
    public FactionMember rumorSpreaderfactionMember;
    public float reliability;
    public bool needsEvaluation;
    public FactionMember deedDoerfactionMember;
    public TypeDeed type;
    public FactionMember deedTargetfactionMember;
}