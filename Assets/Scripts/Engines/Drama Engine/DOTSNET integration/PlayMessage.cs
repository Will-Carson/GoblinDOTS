using DOTSNET;

public struct PlayMessage : NetworkMessage
{
    public int playId;
    public ushort GetID() { return 0x1002; }

    public bool Deserialize(ref SegmentReader reader)
    {
        return reader.ReadInt(out playId);
    }

    public bool Serialize(ref SegmentWriter writer)
    {
        return writer.WriteInt(playId);
    }
}
