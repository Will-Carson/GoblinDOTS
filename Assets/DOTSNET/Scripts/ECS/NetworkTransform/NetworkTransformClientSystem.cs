// Broadcasts position+rotation from client to server.
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    // note: [AlwaysUpdateSystem] isn't needed because we should only broadcast
    //       if there are entities around.
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    // ComponentSystem because Jobs can't send packets
    public class NetworkTransformClientSystem : SystemBase
    {
        // dependencies
        [AutoAssign] protected NetworkClientSystem client;

        // send state to server every 100ms
        // (modified by NetworkServerSystemAuthoring component)
        public float interval = 0.1f;
        double lastSendTime;

        void Send()
        {
            // for each NetworkEntity
            Entities.ForEach((Entity entity,
                              Translation translation,
                              Rotation rotation,
                              NetworkEntity networkEntity,
                              NetworkTransform networkTransform) =>
            {
                // only if client authority
                if (networkTransform.syncDirection != SyncDirection.CLIENT_TO_SERVER)
                    return;

                // only for objects owned by this connection
                if (!networkEntity.owned)
                    return;

                // create the message
                TransformMessage message = new TransformMessage(
                    networkEntity.netId,
                    translation.Value,
                    rotation.Value
                );

                // send it
                client.Send(message);
            })
            .WithoutBurst()
            .Run();
        }

        // update sends state every couple of seconds
        protected override void OnUpdate()
        {
            if (Time.ElapsedTime >= lastSendTime + interval)
            {
                Send();
                lastSendTime = Time.ElapsedTime;
            }
        }
    }
}
