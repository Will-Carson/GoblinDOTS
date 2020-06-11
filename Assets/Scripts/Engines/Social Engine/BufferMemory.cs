using Unity.Entities;

public struct Memory : IBufferElementData
{
    public FactionMember rumorSpreaderFactionMember;
    public FactionMember deedDoerFactionMember;
    public TypeDeed type;
    public FactionMember deedTargetFactionMember;
    public int timesCommitted;
    public float impact;
    public float reliability;
}