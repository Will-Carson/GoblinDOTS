﻿using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using DOTSNET;

namespace Apathy
{
    [ClientWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ApathyTransportClientSystem : TransportClientSystem
    {
        // configuration
        public ushort Port = 7777;
        public bool NoDelay = true;
        public int MaxMessageSize = 16 * 1024;
        // for large ECS worlds, 1k/tick is not enough:
        public int MaxReceivesPerTick = 100000;

        // client
        Client client = new Client();

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
        public override bool IsConnected() => client.Connected;
        public override void Connect(string address) => client.Connect(address, Port);
        public override bool Send(ArraySegment<byte> segment) => client.Send(segment);
        public override void Disconnect() => client.Disconnect();

        // ECS /////////////////////////////////////////////////////////////////
        protected override void OnCreate()
        {
            // configure
            client.NoDelay = NoDelay;
            client.MaxMessageSize = MaxMessageSize;
            client.MaxReceivesPerTickPerConnection = MaxReceivesPerTick;
            Debug.Log("ApathyTransportClientSystem initialized!");
        }

        protected override void OnUpdate()
        {
            client.GetNextMessages(queue);
            while (queue.Count > 0)
            {
                Message message = queue.Dequeue();
                switch (message.eventType)
                {
                    case EventType.Connected:
                        OnConnected();
                        break; // breaks switch, not while
                    case EventType.Data:
                        OnData(message.data);
                        break; // breaks switch, not while
                    case EventType.Disconnected:
                        OnDisconnected();
                        break; // breaks switch, not while
                }
            }
        }

        protected override void OnDestroy()
        {
            Debug.Log("ApathyTransportClientSystem shutdown!");
            client.Disconnect();
        }
    }
}