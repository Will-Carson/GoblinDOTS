using DOTSNET;

public struct DialogueMessage : NetworkMessage
{
    public int dialogueId;
    public ushort GetID() { return 0x1001; }

    public bool Deserialize(ref SegmentReader reader)
    {
        return reader.ReadInt(out dialogueId);
    }

    public bool Serialize(ref SegmentWriter writer)
    {
        return writer.WriteInt(dialogueId);
    }
}
