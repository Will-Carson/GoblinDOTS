// A message that synchronizes a NetworkEntity's position+rotation (=transform)
using Unity.Mathematics;

namespace DOTSNET
{
    public struct TransformMessage : NetworkMessage
    {
        // client needs to identify the entity by netId
        public ulong netId;

        // the position
        public float3 position;

        // the rotation
        public quaternion rotation;

        public ushort GetID() { return 0x0025; }

        public TransformMessage(ulong netId, float3 position, quaternion rotation)
        {
            this.netId = netId;
            this.position = position;
            this.rotation = rotation;
        }

        public bool Serialize(ref SegmentWriter writer)
        {
            return writer.WriteULong(netId) &&
                   writer.WriteFloat3(position) &&
                   writer.WriteQuaternion(rotation);
        }

        public bool Deserialize(ref SegmentReader reader)
        {
            return reader.ReadULong(out netId) &&
                   reader.ReadFloat3(out position) &&
                   reader.ReadQuaternion(out rotation);
        }
    }
}