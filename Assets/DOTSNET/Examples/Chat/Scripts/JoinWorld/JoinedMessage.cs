namespace DOTSNET.Examples.Chat
{
    public struct JoinedMessage : NetworkMessage
    {
        public ushort GetID() { return 0x1002; }
        public bool Serialize(ref SegmentWriter writer) { return true; }
        public bool Deserialize(ref SegmentReader reader) { return true; }
    }
}