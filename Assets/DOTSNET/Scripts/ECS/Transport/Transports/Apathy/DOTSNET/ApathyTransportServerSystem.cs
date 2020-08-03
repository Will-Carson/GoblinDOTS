using System;
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
        // Apathy is buffer limited. NoDelay should always be enabled.
        public bool NoDelay = true;
        // for large ECS worlds, 100/tick is not enough:
        public int MaxReceivesPerTickPerConnection = 1000;

        // server
        internal Server server = new Server();

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
        public override bool IsActive() => server.Active;
        public override void Start() => server.Start(Port);
        // note: DOTSNET already packs messages. Transports don't need to.
        public override bool Send(int connectionId, ArraySegment<byte> segment, Channel channel) => server.Send(connectionId, segment);
        public override bool Disconnect(int connectionId) => server.Disconnect(connectionId);
        public override string GetAddress(int connectionId) => server.GetClientAddress(connectionId);
        public override void Stop()
        {
            server.Stop();
        }

        // ECS /////////////////////////////////////////////////////////////////
        // configure Apathy in OnStartRunning. OnCreate is too early because
        // settings are still applied from Authoring.Awake after OnCreate.
        protected override void OnStartRunning()
        {
            // configure
            server.NoDelay = NoDelay;
            server.MaxReceivesPerTickPerConnection = MaxReceivesPerTickPerConnection;

            // set up events
            server.OnConnected = (connectionId) =>
            {
                OnConnected(connectionId);
            };
            server.OnData = OnData;
            server.OnDisconnected = (connectionId) =>
            {
                OnDisconnected(connectionId);
            };

            Debug.Log("ApathyTransportServerSystem initialized!");
        }

        protected override void OnUpdate()
        {
            if (server.Active)
            {
                // process incoming messages
                server.Update();
            }
        }

        protected override void OnDestroy()
        {
            Debug.Log("ApathyTransportServerSystem shutdown!");
            server.Stop();
        }
    }
}
