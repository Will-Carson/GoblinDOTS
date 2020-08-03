using System;
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
        // Apathy is buffer limited. NoDelay should always be enabled.
        public bool NoDelay = true;
        // for large ECS worlds, 1k/tick is not enough:
        public int MaxReceivesPerTick = 100000;

        // client
        internal Client client = new Client();

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
        public override int GetMaxPacketSize() => Common.MaxMessageSize;
        public override bool IsConnected() => client.Connected;
        public override void Connect(string address) => client.Connect(address, Port);
        public override bool Send(ArraySegment<byte> segment, Channel channel) => client.Send(segment);
        public override void Disconnect() => client.Disconnect();

        // ECS /////////////////////////////////////////////////////////////////
        // configure Apathy in OnStartRunning. OnCreate is too early because
        // settings are still applied from Authoring.Awake after OnCreate.
        protected override void OnStartRunning()
        {
            // configure
            client.NoDelay = NoDelay;
            client.MaxReceivesPerTickPerConnection = MaxReceivesPerTick;

            // set up events
            client.OnConnected = OnConnected;
            client.OnData = OnData;
            client.OnDisconnected = OnDisconnected;

            Debug.Log("ApathyTransportClientSystem initialized!");
        }

        protected override void OnUpdate()
        {
            client.Update();
        }

        protected override void OnDestroy()
        {
            Debug.Log("ApathyTransportClientSystem shutdown!");
            client.Disconnect();
        }
    }
}