// Broadcasts position+rotation from server to client.
//
// Benchmark: 10k Entities, max distance, interval=0, memory transport, no cam
//
//    ____________________|_System_Time_|
//    Run() without Burst |  20-22 ms   |
//    Run() with    Burst |    5-6 ms   |
//
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class NetworkTransformServerSystem : NetworkBroadcastSystem
    {
        // NativeMultiMap so we can run most of it with Burst enabled
        NativeMultiHashMap<int, TransformMessage> messages;

        protected override void OnCreate()
        {
            // call base because it might be implemented.
            base.OnCreate();

            // allocate
            messages = new NativeMultiHashMap<int, TransformMessage>(1000, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            // dispose
            messages.Dispose();

            // call base because it might be implemented.
            base.OnDestroy();
        }

        protected override void Broadcast()
        {
            // run with Burst
            NativeMultiHashMap<int, TransformMessage> _messages = messages;
            Entities.ForEach((in DynamicBuffer<NetworkObserver> observers,
                              in Translation translation,
                              in Rotation rotation,
                              in NetworkEntity networkEntity,
                              in NetworkTransform networkTransform) =>
            {
                // TransformMessage is the same one for each observer.
                // let's create it only once, which is faster.
                TransformMessage message = new TransformMessage(
                    networkEntity.netId,
                    translation.Value,
                    rotation.Value
                );

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
                        // add to messages and send afterwards without burst
                        _messages.Add(connectionId, message);
                    }
                }
            })
            .Run();

            // send after the ForEach. this way we can run ForEach with Burst(!)
            // => send unreliable if possible since we'll send again next time.
            server.Send(_messages, Channel.Unreliable);
            messages.Clear();
        }
    }
}
