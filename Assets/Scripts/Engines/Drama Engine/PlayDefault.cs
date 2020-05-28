using UnityEngine;
using System.Collections;

public struct PlayRequirementsDefault : IPlayRequirements
{
    public bool Requirements()
    {
        return true;
    }
}
