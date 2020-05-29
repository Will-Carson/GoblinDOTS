using UnityEngine;
using System.Collections;

public struct PlayRequirementsDefault : IPlayRequirements
{
    public bool Requirements(out EventPlayRequest playRequest)
    {
        playRequest = new EventPlayRequest();
        return true;
    }
}
