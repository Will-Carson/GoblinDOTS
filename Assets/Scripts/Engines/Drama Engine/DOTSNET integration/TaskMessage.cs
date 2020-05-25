using DOTSNET;

public struct TaskMessage : NetworkMessage
{
    public int taskId;
    public ushort GetID() { return 0x1004; }

    public bool Deserialize(ref SegmentReader reader)
    {
        return reader.ReadInt(out taskId);
    }

    public bool Serialize(ref SegmentWriter writer)
    {
        return writer.WriteInt(taskId);
    }
}
