using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using DOTSNET;

[ServerWorld]
public class ProcessDialogueRequest : NetworkBroadcastSystem
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS = null;

    // NativeMultiMap so we can run most of it with Burst enabled
    NativeMultiHashMap<int, DialogueMessage> messages;
    NativeList<DialogueMessage> messagesList;

    protected override void OnCreate()
    {
        // call base because it might be implemented.
        base.OnCreate();

        // allocate
        messages = new NativeMultiHashMap<int, DialogueMessage>(1000, Allocator.Persistent);
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
        var ecb = ESECBS.CreateCommandBuffer();

        // run with Burst
        var _messages = messages;
        Entities.ForEach((Entity entity,
                          ref DynamicBuffer<DialogueRequest> dialogueRequests,
                          in DynamicBuffer<NetworkObserver> observers,
                          in NetworkEntity networkEntity) =>
        {
            // TransformMessage is the same one for each observer.
            // let's create it only once, which is faster.
            for (int j = 0; j < dialogueRequests.Length; j++)
            {
                if (dialogueRequests[j].sent > 3) continue;
                var dr = dialogueRequests[j];
                dr.sent = dr.sent + 1;
                dialogueRequests[j] = dr;

                DialogueMessage message = new DialogueMessage
                {
                    actorId = dialogueRequests[j].actorId,
                    dialogueId = dialogueRequests[j].dialogueId
                };

                // send state to each observer connection
                // DynamicBuffer foreach allocates. use for.
                for (int i = 0; i < observers.Length; ++i)
                {
                    // get connectionId
                    int connectionId = observers[i];

                    // owner?
                    bool owner = networkEntity.connectionId == connectionId;

                    // add to messages and send afterwards without burst
                    _messages.Add(connectionId, message);
                }
            }
        })
        .Run();

        // for each connectionId:
        foreach (int connectionId in server.connections.Keys)
        {
            // sort messages into NativeList and batch send them.
            // (NativeMultiMap.GetKeyArray allocates, so we simply iterate each
            //  connectionId on the server and assume that most of them will
            //  receive a message anyway)
            NativeMultiHashMapIterator<int>? it = default;
            while (messages.TryIterate(connectionId, out DialogueMessage message, ref it))
            {
                server.Send(connectionId, message, Channel.Reliable);
            }
        }

        // clean up
        messages.Clear();

        Dependency.Complete();
        ESECBS.AddJobHandleForProducer(Dependency);
    }
}

public struct DialogueRequest : IBufferElementData
{
    public int actorId;
    public int dialogueId;
    public int sent;
}

public struct DialogueMessage : NetworkMessage
{
    public ushort GetID() { return 0x1001; }
    public int actorId;
    public int dialogueId;
}