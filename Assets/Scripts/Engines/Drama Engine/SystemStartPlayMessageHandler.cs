using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using DOTSNET;

public class SystemUpdatePlayMessageHandler : NetworkClientMessageSystem<UpdatePlayServerMessage>
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    NativeHashMap<ulong, UpdatePlayServerMessage> messages = new NativeHashMap<ulong, UpdatePlayServerMessage>(1000, Allocator.Persistent);

    protected override void OnMessage(UpdatePlayServerMessage message)
    {
        messages[message.netId] = message;
    }

    protected override void OnUpdate()
    {
        var buffer = ESECBS.CreateCommandBuffer();

        var _messages = messages;
        Entities.ForEach((Entity entity,
                          in StageId stageId,
                          in NetworkEntity networkEntity) =>
        {
            if (_messages.ContainsKey(networkEntity.netId))
            {
                var message = _messages[networkEntity.netId];
                var c = new PlayDataComponent()
                {
                    data = new DataRunningPlay()
                    {
                        // TODO update subjects
                        currentLineId = message.data.currentLineId,
                        lastLineId = message.data.lastLineId,
                        lastUpdated = message.data.lastUpdated,
                        playId = message.data.playId,
                        stageId = message.data.stageId,
                        subjectX = message.data.subjectX,
                        subjectY = message.data.subjectY,
                        subjectZ = message.data.subjectZ
                    }
                };
                buffer.AddComponent(entity, c);
            }
        })
        .WithBurst()
        .Run();
    }
}
