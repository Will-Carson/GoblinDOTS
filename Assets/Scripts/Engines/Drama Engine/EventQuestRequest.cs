using System;

public struct EventQuestRequest : IEquatable<EventQuestRequest>
{
    public int requesterId;
    public int giverId;

    public bool Equals(EventQuestRequest other)
    {
        if (requesterId == other.requesterId && giverId == other.giverId)
        {
            return true;
        }

        return false;
    }
}