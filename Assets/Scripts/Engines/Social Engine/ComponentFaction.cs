using Unity.Collections;
using Unity.Entities;

public struct Faction : IComponentData
{
    public int id;
    public DataValues values;
}