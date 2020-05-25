// Broadcasts position+rotation from server to client.
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    public class NetworkTransformServerSystem : NetworkBroadcastSystem
    {
        protected override void Broadcast()
        {
            // for each NetworkEntity
            Entities.ForEach((Entity entity,
                              DynamicBuffer<NetworkObserver> observers,
                              Translation translation,
                              Rotation rotation,
                              NetworkEntity networkEntity,
                              NetworkTransform networkTransform) =>
            {
                // send state to each observer connection
                // DynamicBuffer foreach allocates. use for.
                for (int i = 0; i < observers.Length; ++i)
                {
                    // get connectionId
                    int connectionId = observers[i];

                    // owner?
                    bool owner = networkEntity.connectionId == connectionId;

                    // only send if not the owner, or if the owner and in
                    // SERVER_TO_CLIENT mode.
                    //
                    // in CLIENT_TO_SERVER mode the owner sends to us, and all
                    // we do is broadcast to everyone but the owner.
                    if (!owner || networkTransform.syncDirection == SyncDirection.SERVER_TO_CLIENT)
                    {
                        // create the message
                        TransformMessage message = new TransformMessage(
                            networkEntity.netId,
                            translation.Value,
                            rotation.Value
                        );

                        // send it
                        server.Send(message, connectionId);
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }
    }
}
