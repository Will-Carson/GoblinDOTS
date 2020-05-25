using System;
using UnityEngine;
using DOTSNET;

namespace Apathy
{
    public class ApathyTransportServerAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkServerSystem in ECS world
        ApathyTransportServerSystem server =>
            Bootstrap.ServerWorld.GetExistingSystem<ApathyTransportServerSystem>();

        // common
        public ushort Port = 7777;
        public bool NoDelay = true;
        public int MaxMessageSize = 16 * 1024;
        public int MaxReceivesPerTickPerConnection = 100;

        // add to selectively created systems before Bootstrap is called
        public Type GetSystemType() => typeof(ApathyTransportServerSystem);

        // apply configuration in awake
        void Awake()
        {
            server.Port = Port;
            server.NoDelay = NoDelay;
            server.MaxMessageSize = MaxMessageSize;
            server.MaxReceivesPerTickPerConnection = MaxReceivesPerTickPerConnection;
        }
    }
}