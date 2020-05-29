using UnityEngine;
using System.Collections;

public struct PlayRequirementsDefault : IPlayRequirement
{
    public bool Requirements(out EventPlayRequest playRequest, WorldStateData worldState)
    {
        playRequest = new EventPlayRequest();
        return true;
    }
}
