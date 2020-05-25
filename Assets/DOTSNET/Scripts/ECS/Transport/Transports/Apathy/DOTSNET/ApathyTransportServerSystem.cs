using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using DOTSNET;

namespace Apathy
{
    [ServerWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ApathyTransportServerSystem : TransportServerSystem
    {
        // configuration
        public ushort Port = 7777;
        public bool NoDelay = true;
        public int MaxMessageSize = 16 * 1024;
        // for large ECS worlds, 100/tick is not enough:
        public int MaxReceivesPerTickPerConnection = 1000;

        // server
        Server server = new Server();

        // cache GetNextMessages queue to avoid allocations
        // -> with capacity to avoid rescaling as long as possible!
        Queue<Message> queue = new Queue<Message>(1000);

        // overrides ///////////////////////////////////////////////////////////
        public override bool Available()
        {
            return Application.platform == RuntimePlatform.OSXEditor ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.LinuxEditor ||
                   Application.platform == RuntimePlatform.LinuxPlayer;
        }

        public override int GetMaxPacketSize() => MaxMessageSize;
        public override bool IsActive() => server.Active;
        public override void Start() => server.Start(Port);
        public override bool Send(int connectionId, ArraySegment<byte> segment) => server.Send(connectionId, segment);
        public override bool Disconnect(int connectionId) => server.Disconnect(connectionId);
        public override string GetAddress(int connectionId) => server.GetClientAddress(connectionId);
        public override void Stop() => server.Stop();

        // ECS /////////////////////////////////////////////////////////////////
        protected override void OnCreate()
        {
            // configure
            server.NoDelay = NoDelay;
            server.MaxMessageSize = MaxMessageSize;
            server.MaxReceivesPerTickPerConnection = MaxReceivesPerTickPerConnection;
            Debug.Log("ApathyTransportServerSystem initialized!");
        }

        protected override void OnUpdate()
        {
            if (server.Active)
            {
                server.GetNextMessages(queue);
                while (queue.Count > 0)
                {
                    Message message = queue.Dequeue();
                    switch (message.eventType)
                    {
                        case EventType.Connected:
                            OnConnected.Invoke(message.connectionId);
                            break; // breaks switch, not while
                        case EventType.Data:
                            OnData.Invoke(message.connectionId, message.data);
                            break; // breaks switch, not while
                        case EventType.Disconnected:
                            OnDisconnected.Invoke(message.connectionId);
                            break; // breaks switch, not while
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            Debug.Log("ApathyTransportServerSystem shutdown!");
            server.Stop();
        }
    }
}