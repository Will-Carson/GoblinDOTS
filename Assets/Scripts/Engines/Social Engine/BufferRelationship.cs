using Unity.Collections;
using Unity.Entities;

public struct Relationship : IBufferElementData
{
    public Faction targetFaction;
    public float affinity;
    public RelationshipValues values;
}