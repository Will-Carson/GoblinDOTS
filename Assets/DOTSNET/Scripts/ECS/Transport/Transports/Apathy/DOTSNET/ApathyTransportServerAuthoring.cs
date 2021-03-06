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
        public int MaxReceivesPerTickPerConnection = 100;
        // Apathy is buffer limited. NoDelay should always be enabled.
        //public bool NoDelay = true;

        // add to selectively created systems before Bootstrap is called
        public Type GetSystemType() => typeof(ApathyTransportServerSystem);

        // apply configuration in awake
        void Awake()
        {
            server.Port = Port;
            //server.NoDelay = NoDelay;
            server.MaxReceivesPerTickPerConnection = MaxReceivesPerTickPerConnection;
        }
    }
}