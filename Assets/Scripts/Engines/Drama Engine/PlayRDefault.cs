using UnityEngine;
using System.Collections;

public struct PlayRDefault : IPlayRequirement
{
    public bool Requirements(out EventPlayRequest playRequest, DataWorldState worldState)
    {
        playRequest = new EventPlayRequest();
        return true;
    }
}
