using DOTSNET;

public struct QuestMessage : NetworkMessage
{
    public int questId;
    public ushort GetID() { return 0x1003; }

    public bool Deserialize(ref SegmentReader reader)
    {
        return reader.ReadInt(out questId);
    }

    public bool Serialize(ref SegmentWriter writer)
    {
        return writer.WriteInt(questId);
    }
}
