// A message that lets the NetworkClient know that an Entity needs to be
// unspawned.
namespace DOTSNET
{
    public struct UnspawnMessage : NetworkMessage
    {
        // client needs to identify the entity by netId
        public ulong netId;

        public ushort GetID() { return 0x0023; }

        public UnspawnMessage(ulong netId)
        {
            this.netId = netId;
        }

        public bool Serialize(ref SegmentWriter writer)
        {
            return writer.WriteULong(netId);
        }

        public bool Deserialize(ref SegmentReader reader)
        {
            return reader.ReadULong(out netId);
        }
    }
}