// Applies the TransformMessage to the Entity.
// There is no interpolation yet, only the bare minimum.
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    public class TransformClientMessageSystem : NetworkClientMessageSystem<TransformMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(TransformMessage message)
        {
            // find entity by netId
            if (client.spawned.TryGetValue(message.netId, out Entity entity))
            {
                // apply position & rotation
                SetComponent(entity, new Translation{Value = message.position});
                SetComponent(entity, new Rotation{Value = message.rotation});
            }
        }
    }
}
