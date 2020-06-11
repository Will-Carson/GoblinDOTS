using Unity.Collections;
using Unity.Entities;

public struct FactionMember : IComponentData
{
    public int id;
    public Faction faction;
    public Mood mood;
    public float power;
}