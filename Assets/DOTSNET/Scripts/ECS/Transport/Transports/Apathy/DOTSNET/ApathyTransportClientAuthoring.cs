using System;
using UnityEngine;
using DOTSNET;

namespace Apathy
{
    public class ApathyTransportClientAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkServerSystem in ECS world
        ApathyTransportClientSystem client =>
            Bootstrap.ClientWorld.GetExistingSystem<ApathyTransportClientSystem>();

        // common
        public ushort Port = 7777;
        public int MaxReceivesPerTick = 1000;
        // Apathy is buffer limited. NoDelay should always be enabled.
        //public bool NoDelay = true;

        // add system if Authoring is used
        public Type GetSystemType() => typeof(ApathyTransportClientSystem);

        // apply configuration in awake
        void Awake()
        {
            client.Port = Port;
            //client.NoDelay = NoDelay;
            client.MaxReceivesPerTick = MaxReceivesPerTick;
        }
    }
}