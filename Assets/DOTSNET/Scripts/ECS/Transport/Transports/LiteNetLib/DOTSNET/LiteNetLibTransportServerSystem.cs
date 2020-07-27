// see https://revenantx.github.io/LiteNetLib/index.html for usage example
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using LiteNetLib;

namespace DOTSNET.LiteNetLib
{
    [ServerWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class LiteNetLibTransportServerSystem : TransportServerSystem
    {
        // configuration
        public ushort Port = 8888;
        public int UpdateTime = 15;
        public int DisconnectTimeout = 5000;

        // LiteNetLib state
        NetManager server;
        Dictionary<int, NetPeer> connections = new Dictionary<int,NetPeer>(1000);

        public override bool Available()
        {
            // all except WebGL
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }

        public override int GetMaxPacketSize()
        {
            // TODO this assumes unreliable
            // TODO use Host.FirstPeer.Mtu later. for now, 1400 should do.
            return 1400;
        }

        public override bool IsActive()
        {
            return server != null;
        }

        public override void Start()
        {
            // not if already started
            if (server != null)
            {
                Debug.LogWarning("LiteNetLib: server already started.");
                return;
            }

            Debug.Log("LiteNet SV: starting...");

            // create server
            EventBasedNetListener listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.UpdateTime = UpdateTime;
            server.DisconnectTimeout = DisconnectTimeout;

            // set up events
            listener.ConnectionRequestEvent += request =>
            {
                //if(server.PeersCount < 10 /* max connections */)
                //    request.AcceptIfKey("SomeConnectionKey");
                //else
                //    request.Reject();
                Debug.Log("LiteNet SV connection request");
                request.AcceptIfKey("DOTSNET_LITENETLIB");
            };
            listener.PeerConnectedEvent += peer =>
            {
                //Debug.Log("LiteNet SV client connected: " + peer.EndPoint + " id=" + peer.Id);
                connections[peer.Id] = peer;
                OnConnected(peer.Id);
            };
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                //Debug.Log("LiteNet SV received " + dataReader.AvailableBytes + " bytes. method=" + deliveryMethod);
                OnData(fromPeer.Id, dataReader.GetRemainingBytesSegment());
                dataReader.Recycle();
            };
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                // this is called both when a client disconnects, and when we
                // disconnect a client.
                //Debug.Log("LiteNet SV client disconnected: " + peer.EndPoint + " info=" + info);
                OnDisconnected(peer.Id);
                connections.Remove(peer.Id);
            };
            listener.NetworkErrorEvent += (point, error) =>
            {
                Debug.LogWarning("LiteNet SV network error: " + point + " error=" + error);
                // TODO should we disconnect or is it called automatically?
            };

            // start listening
            server.Start(Port);
        }

        public override bool Send(int connectionId, ArraySegment<byte> segment, Channel channel)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    try
                    {
                        // convert DOTSNET channel to LiteNetLib channel & send
                        DeliveryMethod deliveryMethod = LiteNetLibTransportUtils.ConvertChannel(channel);
                        peer.Send(segment.Array, segment.Offset, segment.Count, deliveryMethod);
                        return true;
                    }
                    catch (TooBigPacketException exception)
                    {
                        Debug.LogWarning("LiteNet SV: send failed for connectionId=" + connectionId + " reason=" + exception);
                        return false;
                    }
                }
                Debug.LogWarning("LiteNet SV: invalid connectionId=" + connectionId);
                return false;
            }
            Debug.LogWarning("LiteNet SV: can't send because not started yet.");
            return false;
        }

        public override bool Disconnect(int connectionId)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    // disconnect the client.
                    // PeerDisconnectedEvent will call OnDisconnect.
                    peer.Disconnect();
                    return true;
                }
                Debug.LogWarning("LiteNet SV: invalid connectionId=" + connectionId);
                return false;
            }
            return false;
        }

        public override string GetAddress(int connectionId)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    return peer.EndPoint.Address.ToString();
                }
            }
            return "";
        }

        public override void Stop()
        {
            if (server != null)
            {
                server.Stop();
                server = null;
            }
        }

        // ECS /////////////////////////////////////////////////////////////////
        protected override void OnUpdate()
        {
            if (server != null)
            {
                server.PollEvents();
            }
        }
    }
}