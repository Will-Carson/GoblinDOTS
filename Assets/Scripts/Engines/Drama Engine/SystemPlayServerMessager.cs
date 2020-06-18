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

    NativeMultiHashMap<int, StartPlayServerMessage> startMessages
        = new NativeMultiHashMap<int, StartPlayServerMessage>(1000, Allocator.Persistent);
    NativeMultiHashMap<int, ContinuePlayServerMessage> continueMessages
        = new NativeMultiHashMap<int, ContinuePlayServerMessage>(1000, Allocator.Persistent);
    NativeMultiHashMap<int, EndPlayServerMessage> endMessages
        = new NativeMultiHashMap<int, EndPlayServerMessage>(1000, Allocator.Persistent);

    protected override void OnDestroy()
    {
        startMessages.Dispose();
        continueMessages.Dispose();
        endMessages.Dispose();
    }

    protected override void Broadcast()
    {
        var buffer = ESECBS.CreateCommandBuffer();
        var time = Time.DeltaTime;
        #region Send messages to play observers

        NativeMultiHashMap<int, StartPlayServerMessage> _startMessages = startMessages;
        NativeMultiHashMap<int, ContinuePlayServerMessage> _continueMessages = continueMessages;
        NativeMultiHashMap<int, EndPlayServerMessage> _endMessages = endMessages;

        // Send start play updates to clients
        Entities
            .ForEach((Entity entity,
                      in StartPlayRequest startPlayRequest,
                      in DynamicBuffer<NetworkObserver> observers,
                      in NetworkEntity networkEntity) =>
            {
                var message = new StartPlayServerMessage();
                message.netId = networkEntity.netId;
                message.stageId = startPlayRequest.stageId;
                message.playId = startPlayRequest.playId;

                for (var i = 0; i < observers.Length; i++)
                {
                    int connectionId = observers[i];
                    _startMessages.Add(connectionId, message);
                }
                buffer.RemoveComponent(entity, (typeof(StartPlayRequest)));
            })
            .WithBurst()
            .Run();

        // Send continue play updates to clients
        Entities
            .ForEach((Entity entity,
                      in ContinuePlayRequest continueRequest,
                      in DynamicBuffer<NetworkObserver> observers,
                      in NetworkEntity networkEntity) =>
            {
                var message = new ContinuePlayServerMessage();
                message.netId = networkEntity.netId;
                message.stageId = continueRequest.stageId;
                message.nextLineId = continueRequest.nextLine;

                for (var i = 0; i < observers.Length; i++)
                {
                    int connectionId = observers[i];
                    _continueMessages.Add(connectionId, message);
                }
                buffer.RemoveComponent(entity, (typeof(ContinuePlayRequest)));
            })
            .WithBurst()
            .Run();

        // Send end play updates to clients
        Entities
            .ForEach((Entity entity,
                      in EndPlayRequest endPlayRequest,
                      in DynamicBuffer<NetworkObserver> observers,
                      in NetworkEntity networkEntity) =>
            {
                var message = new EndPlayServerMessage();
                message.netId = networkEntity.netId;
                message.stageId = endPlayRequest.stageId;

                for (var i = 0; i < observers.Length; i++)
                {
                    int connectionId = observers[i];
                    _endMessages.Add(connectionId, message);
                }
                buffer.RemoveComponent(entity, (typeof(EndPlayRequest)));
            })
            .WithBurst()
            .Run();

        #endregion

        server.Send(startMessages);
        server.Send(continueMessages);
        server.Send(endMessages);

        startMessages.Clear();
        continueMessages.Clear();
        endMessages.Clear();
    }
}