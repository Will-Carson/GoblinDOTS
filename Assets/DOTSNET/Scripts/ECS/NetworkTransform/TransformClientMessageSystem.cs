// Applies the TransformMessage to the Entity.
// There is no interpolation yet, only the bare minimum.
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    public class TransformClientMessageSystem : NetworkClientMessageSystem<TransformMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(NetworkMessage message)
        {
            // find entity by netId
            TransformMessage transformMessage = (TransformMessage)message;
            if (client.spawned.TryGetValue(transformMessage.netId, out Entity entity))
            {
                // apply position & rotation
                SetComponent(entity, new Translation{Value = transformMessage.position});
                SetComponent(entity, new Rotation{Value = transformMessage.rotation});
            }
        }
    }
}
