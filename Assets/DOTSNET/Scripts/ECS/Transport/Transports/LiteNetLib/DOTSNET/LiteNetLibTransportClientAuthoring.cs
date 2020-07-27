using System;
using UnityEngine;

namespace DOTSNET.LiteNetLib
{
    public class LiteNetLibTransportClientAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkClientSystem in ECS world
        LiteNetLibTransportClientSystem client =>
            Bootstrap.ClientWorld.GetExistingSystem<LiteNetLibTransportClientSystem>();

        // common
        public ushort Port = 8888;
        [Tooltip("Library logic update and send period in milliseconds")]
        public int UpdateTime = 15;
        [Tooltip("If NetManager doesn't receive any packet from remote peer during this time then connection will be closed")]
        public int DisconnectTimeout = 5000;

        // add to selectively created systems before Bootstrap is called
        public Type GetSystemType() => typeof(LiteNetLibTransportClientSystem);

        // apply configuration in awake
        void Awake()
        {
            client.Port = Port;
            client.UpdateTime = UpdateTime;
            client.DisconnectTimeout = DisconnectTimeout;
        }

        /*void OnGUI()
        {
            if (GUI.Button(new Rect(10, 100, 100, 15), "CL SEND"))
            {
                client.Send(new ArraySegment<byte>(new byte[]{0x01, 0x02}));
            }
        }*/
    }
}