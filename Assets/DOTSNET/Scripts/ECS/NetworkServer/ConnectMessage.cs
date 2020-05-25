// ConnectMessage is an artificial message.
// It is never sent over the network. It is only used to register a handler.
namespace DOTSNET
{
    public struct ConnectMessage : NetworkMessage
    {
        public ushort GetID() { return 0x0001; }
        public bool Serialize(ref SegmentWriter writer) { return true; }
        public bool Deserialize(ref SegmentReader reader) { return true; }
    }
}