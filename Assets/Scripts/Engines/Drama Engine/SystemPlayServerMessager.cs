using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

// TODO PEL has no place here. This system isn't actually running the plays, so the PEL doesn't matter.
[ServerWorld]
public class SystemPlayServerMessager : NetworkBroadcastSystem
{
    // NativeMultiMap so we can run most of it with Burst enabled
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    
    NativeMultiHashMap<int, UpdatePlayServerMessage> updateMessages
        = new NativeMultiHashMap<int, UpdatePlayServerMessage>(1000, Allocator.Persistent);

    protected override void OnDestroy()
    {
        updateMessages.Dispose();
    }

    protected override void Broadcast()
    {
        var buffer = ESECBS.CreateCommandBuffer();
        var time = Time.DeltaTime;

        #region Prepare messages for play observers

        NativeMultiHashMap<int, UpdatePlayServerMessage> _updateMessages = updateMessages;

        // Prepare play update messages to be sent
        Entities
            .ForEach((Entity entity,
                      in UpdatePlayRequest updateRequest,
                      in DynamicBuffer<NetworkObserver> observers,
                      in NetworkEntity networkEntity) =>
            {
                var message = new UpdatePlayServerMessage();

                message.netId = networkEntity.netId;
                message.data = updateRequest.data;

                for (var i = 0; i < observers.Length; i++)
                {
                    int connectionId = observers[i];
                    _updateMessages.Add(connectionId, message);
                }
                buffer.RemoveComponent(entity, (typeof(UpdatePlayRequest)));
            })
            .WithBurst()
            .Run();

        #endregion
        
        server.Send(updateMessages);
        updateMessages.Clear();
    }
}