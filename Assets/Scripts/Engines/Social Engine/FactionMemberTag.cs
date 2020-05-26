using System;
using System.Collections.Generic;
using Unity.Entities;

[Serializable]
public struct FactionMemberComponent : IComponentData
{
    public int id;
}
