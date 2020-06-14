// A message that sends the full state of a NetworkEntity to the client in order
// to spawn it.
using Unity.Collections;
using Unity.Mathematics;

namespace DOTSNET
{
    public struct SpawnMessage : NetworkMessage
    {
        // client needs to know which prefab to spawn
        public Bytes16 prefabId;

        // client needs to know which netId was assigned to this entity
        public ulong netId;

        // flag to indicate if the connection that we send it to owns the entity
        // (byte instead of bool because bool isn't blittable)
        public byte owned;

        // the spawn position
        // unlike StateMessage, we include the position once when spawning so
        // that even without a NetworkTransform system, it's still positioned
        // correctly when spawning.
        public float3 position;

        // the spawn rotation
        // unlike StateMessage, we include the rotation once when spawning so
        // that even without a NetworkTransform system, it's still rotated
        // correctly when spawning.
        public quaternion rotation;

        public ushort GetID() { return 0x0022; }

        public SpawnMessage(Bytes16 prefabId, ulong netId, byte owned, float3 position, quaternion rotation)
        {
            this.prefabId = prefabId;
            this.netId = netId;
            this.owned = owned;
            this.position = position;
            this.rotation = rotation;
        }
    }
}