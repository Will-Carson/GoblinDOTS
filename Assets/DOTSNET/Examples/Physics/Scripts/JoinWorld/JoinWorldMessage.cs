using Unity.Collections;

namespace DOTSNET.Examples.Physics
{
    public struct JoinWorldMessage : NetworkMessage
    {
        public Bytes16 playerPrefabId;

        public ushort GetID() { return 0x1001; }

        public JoinWorldMessage(Bytes16 playerPrefabId)
        {
            this.playerPrefabId = playerPrefabId;
        }

        public bool Serialize(ref SegmentWriter writer)
        {
            return writer.WriteBytes16(playerPrefabId);
        }

        public bool Deserialize(ref SegmentReader reader)
        {
            return reader.ReadBytes16(out playerPrefabId);
        }
    }
}