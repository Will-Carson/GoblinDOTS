using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using DOTSNET;

public class SystemStartPlayMessageHandler : NetworkClientMessageSystem<StartPlayServerMessage>
{
    [AutoAssign] EndSimulationEntityCommandBufferSystem ESECBS;
    NativeHashMap<ulong, StartPlayServerMessage> messages = new NativeHashMap<ulong, StartPlayServerMessage>(1000, Allocator.Persistent);

    protected override void OnMessage(StartPlayServerMessage message)
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
                var c = new RunningPlay()
                {
                    runningPlay = new DataRunningPlay()
                    {
                        currentLineId = 0,
                        lastLineId = 0,
                        lastUpdated = 0,
                        playId = message.playId,
                        stageId = stageId.stageId,
                        subjectX = 0,
                        subjectY = 0,
                        subjectZ = 0
                    }
                };
                buffer.AddComponent(entity, c);
            }
        });
    }
}
