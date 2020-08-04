using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class ProcessDialogueRequest : NetworkBroadcastSystem
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;

    private EntityCommandBuffer Buffer;

    protected override void OnCreate()
    {
        Buffer = ESECBS.CreateCommandBuffer();
    }

    protected override void Broadcast()
    {
        var buffer = Buffer;
        var server2 = server;

        Entities.ForEach((Entity entity,
                          int entityInQueryIndex,
                          DynamicBuffer<NetworkObserver> observers,
                          DynamicBuffer<DialogueRequest> requests,
                          ref StageId stageId) =>
        {
            for (int i = 0; i < observers.Length; i++)
            {
                for (int j = 0; j < requests.Length; j++)
                {
                    var message = new DialogueMessage
                    {
                        actorId = requests[j].actorId,
                        dialogueId = requests[j].dialogueId
                    };
                    var connectionId = observers[i];
                    server2.Send(connectionId, message);
                }
            }
            requests.Clear();
        })
        .WithoutBurst()
        .Run();
    }
}

public struct DialogueRequest : IBufferElementData
{
    public int actorId;
    public int dialogueId;
}

public struct DialogueMessage : NetworkMessage
{
    public ushort GetID() { return 0x1001; }
    public int actorId;
    public int dialogueId;
}