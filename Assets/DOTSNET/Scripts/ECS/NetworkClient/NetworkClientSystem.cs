using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET
{
    public enum ClientState : byte
    {
        DISCONNECTED, CONNECTING, CONNECTED
    }

    // NetworkMessage delegate for clients
    public delegate void NetworkMessageClientDelegate(NetworkMessage message);

    // NetworkClientSystem should be updated AFTER all other client systems.
    // we need a guaranteed update order to avoid race conditions where it might
    // randomly be updated before other systems, causing all kinds of unexpected
    // effects. determinism is always a good idea!
    [ClientWorld]
    // Client may need to apply physics, so update in the safe group
    [UpdateInGroup(typeof(ApplyPhysicsGroup))]
    [UpdateAfter(typeof(ClientConnectedSimulationSystemGroup))]
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class NetworkClientSystem : SystemBase
    {
        // there is only one NetworkClient(System), so keep state in here
        // (using 1 component wouldn't gain us any performance. only complexity)

        // state instead of active/connecting/connected/disconnecting variables.
        // -> less variables to define
        // -> less variables to set in code
        // -> less chance for odd cases like connecting := active && !connected
        // -> easier to test
        // -> Connect/Disconnect early returns are 100% precise. instead of only
        //    checking .active, we can now do early returns while disconnecting,
        //    or connecting is in process too. it's much safer.
        // -> 100% precise
        // -> state dependent logic is way easier to write with state machines!
        public ClientState state { get; private set; } = ClientState.DISCONNECTED;

        // with DOTS it's possible to freeze all NetworkEntities in place after
        // disconnecting, and only clean them up before connecting again.
        // * a lot of MMOs do this after disconnecting, where a player can still
        //   see the world but nobody moves. it's a good user experience.
        // * it can be useful when debugging so we see the exact scene states
        //   right before a disconnect happened.
        public bool disconnectFreezesScene;

        // dependencies
        [AutoAssign] protected PrefabSystem prefabSystem;
        // transport is manually assign via FindAvailable
        TransportClientSystem transport;

        // message handlers
        // -> we use delegates to be as widely usable as possible
        // -> if necessary, a ComponentSystem could provide a delegate that adds
        //    the NetworkMessage as component so that it can be processed by a
        //    (job)ComponentSystem
        // -> KeyValuePair so that we can deserialize into a copy of Network-
        //    Message of the correct type, before calling the handler
        Dictionary<ushort, KeyValuePair<NetworkMessage, NetworkMessageClientDelegate>> handlers =
            new Dictionary<ushort, KeyValuePair<NetworkMessage, NetworkMessageClientDelegate>>();

        // all spawned NetworkEntities visible to this client.
        // for cases where we need to modify one of them. this way we don't have
        // run a query over all of them.
        public Dictionary<ulong, Entity> spawned = new Dictionary<ulong, Entity>();

        // Send serializes messages into ArraySegments, which needs a byte[]
        // we use one for all sends.
        // we initialize it to transport max packet size.
        // we buffer it to have allocation free sends via ArraySegments.
        byte[] sendBuffer;

        // network controls ////////////////////////////////////////////////////
        // Connect tells the client to start connecting.
        // it returns immediately, but it won't be connected immediately.
        // depending on the transport, it could take a little while.
        public void Connect(string address)
        {
            // do nothing if already active (= if connecting or connected)
            if (state != ClientState.DISCONNECTED)
                return;

            // if freezeSceneWhenDisconnecting is enabled then NetworkEntities
            // were not cleaned up in Disconnect. so let's clean them up before
            // connecting no matter what.
            DestroyAllNetworkEntities();

            // connect
            state = ClientState.CONNECTING;
            transport.Connect(address);
        }

        // Disconnect tells the client to disconnect IMMEDIATELY.
        // unlike connecting, it is always possible to disconnect from a network
        // instantly. so by the time this function returns, we are fully
        // disconnected.
        // => make sure that Transport.ClientDisconnect fully disconnected the
        //    client before returning!
        public void Disconnect()
        {
            // do nothing if already offline
            if (state == ClientState.DISCONNECTED)
                return;

            // disconnect
            transport.Disconnect();
            state = ClientState.DISCONNECTED;
            spawned.Clear();

            // only destroy NetworkEntities if the user doesn't want to freeze
            // the scene after disconnects. otherwise they will be cleaned up
            // before the next connect.
            if (!disconnectFreezesScene)
            {
                DestroyAllNetworkEntities();
            }
        }

        // network events called by TransportSystem ////////////////////////////
        // named On'Transport'Connected etc. because OnClientConnected wouldn't
        // be completely obvious that it comes from the transport
        void OnTransportConnected()
        {
            Debug.Log("NetworkClientSystem.OnTransportConnected");
            state = ClientState.CONNECTED;

            // note: we have no authenticated state on the client, because the
            //       client always trusts the server, and the server never
            //       trusts the client.

            // note: we don't invoke a ConnectMessage handler on clients.
            //       systems in ClientConnectedSimulationSystemGroup will start/
            //       stop running when connecting/disconnecting, so use
            //       OnStartRunning to react to a connect instead!
        }

        // segment's array is only valid until returning
        void OnTransportData(ArraySegment<byte> segment)
        {
            //Debug.Log("NetworkClientSystem.OnTransportData: " + BitConverter.ToString(segment.Array, segment.Offset, segment.Count));

            // try to read the message id
            SegmentReader reader = new SegmentReader(segment);
            if (reader.ReadUShort(out ushort messageId))
            {
                //Debug.Log("NetworkClientSystem.OnTransportData messageId: 0x" + messageId.ToString("X4"));

                // create a new message of type messageId by copying the
                // template from the handler. we copy it automatically because
                // messages are value types, so that's a neat trick here.
                if (handlers.TryGetValue(messageId, out KeyValuePair<NetworkMessage, NetworkMessageClientDelegate> kvp))
                {
                    // deserialize message data
                    // IMPORTANT: remember that segment expires after returning,
                    //            so if byte[] payloads are needed then copy them
                    NetworkMessage message = kvp.Key;
                    if (message.Deserialize(ref reader))
                    {
                        // handle it
                        kvp.Value(message);
                    }
                    // invalid message contents are not okay. disconnect.
                    else
                    {
                        Debug.Log("NetworkClientSystem.OnTransportData: deserializing " + message.GetType() + " failed with reader at Position: " + reader.Position + " Remaining: " + reader.Remaining);
                        Disconnect();
                    }
                }
                // unhandled messageIds are not okay. disconnect.
                else
                {
                    Debug.Log("NetworkClientSystem.OnTransportData: unhandled messageId: 0x" + messageId.ToString("X4"));
                    Disconnect();
                }

            }
            // partial message ids are not okay. disconnect.
            else
            {
                Debug.Log("NetworkClientSystem.OnTransportData: failed to fully read messageId for segment with offset: " + segment.Offset + " length: " + segment.Count);
                Disconnect();
            }
        }

        void OnTransportDisconnected()
        {
            Debug.Log("NetworkClientSystem.OnTransportDisconnected");
            // OnTransportDisconnect happens either after we called Disconnect,
            // or after the server disconnected our connection.
            // if the server disconnected us, then we need to set the state!
            state = ClientState.DISCONNECTED;

            // note: we don't invoke a DisconnectMessage handler on clients.
            //       systems in ClientConnectedSimulationSystemGroup will start/
            //       stop running when connecting/disconnecting, so use
            //       OnStopRunning to react to a disconnect instead!
        }

        // messages ////////////////////////////////////////////////////////////
        // send a message to the server
        public void Send<T>(T message) where T : NetworkMessage
        {
            // make sure that we can use the send buffer
            if (sendBuffer?.Length > 0)
            {
                // create the segment writer
                SegmentWriter writer = new SegmentWriter(sendBuffer);

                // write message id
                if (writer.WriteUShort(message.GetID()))
                {
                    // serialize message content
                    if (message.Serialize(ref writer))
                    {
                        // send to transport.
                        // (it will have to free up the segment immediately)
                        if (!transport.Send(writer.segment))
                        {
                            // send can fail if the transport has issues
                            // like full buffers, broken pipes, etc.
                            // so if Send gets called before the next
                            // transport update removes the broken
                            // connection, then we will see a warning.
                            Debug.LogWarning("NetworkClientSystem.Send: failed to send message of type " + typeof(T) + ". This can happen if the connection is broken before the next transport update removes it.");

                            // TODO consider flagging the connection as broken
                            // to only log the warning message once like we do
                            // in NetworkServerSystem.Send
                        }
                    }
                    else Debug.LogWarning("NetworkClientSystem.Send: serializing message of type " + typeof(T) + " failed. Maybe the message is bigger than sendBuffer " + sendBuffer.Length + " bytes?");
                }
                else Debug.LogWarning("NetworkClientSystem.Send: writing messageId of type " + typeof(T) + " failed. Maybe the id is bigger than sendBuffer " + sendBuffer.Length + " bytes?");
            }
            else Debug.LogError("NetworkClientSystem.Send: sendBuffer not initialized or 0 length: " + sendBuffer);
        }

        // register handler for a message.
        // we use 'where NetworkMessage' to make sure it only works for them,
        // and we use 'where new()' so we can create the type at runtime
        // => we use <T> generics so we don't have to pass both messageId and
        //    NetworkMessage template each time. it's just cleaner this way.
        //
        // usage: RegisterHandler<TestMessage>(func);
        public bool RegisterHandler<T>(NetworkMessageClientDelegate handler)
            where T : NetworkMessage, new()
        {
            // create a message template to get id and to copy from
            T template = default;

            // make sure no one accidentally overwrites a handler
            // (might happen in case of duplicate messageIds etc.)
            if (!handlers.ContainsKey(template.GetID()))
            {
                handlers[template.GetID()] = new KeyValuePair<NetworkMessage, NetworkMessageClientDelegate>(template, handler);
                return true;
            }

            // log warning in case we tried to overwrite. could be extremely
            // useful for debugging/development, so we notice right away that
            // a system accidentally called it twice, or that two messages
            // accidentally have the same messageId.
            Debug.LogWarning("NetworkClientSystem: handler for " + typeof(T) + " was already registered.");
            return false;
        }

        // get a handler. this is useful for testing.
        // => we use <T> generics so we don't have to pass messageId  each time.
        //
        // usage: GetHandler<TestMessage>();
        public NetworkMessageClientDelegate GetHandler<T>()
            where T : NetworkMessage, new()
        {
            // create a message template to get id
            T template = default;
            handlers.TryGetValue(template.GetID(), out KeyValuePair<NetworkMessage, NetworkMessageClientDelegate> kvp);
            return kvp.Value;
        }

        // unregister a handler.
        // => we use <T> generics so we don't have to pass messageId  each time.
        public bool UnregisterHandler<T>()
            where T : NetworkMessage, new()
        {
            // create a message template to get id
            T template = default;
            return handlers.Remove(template.GetID());
        }

        // spawn ///////////////////////////////////////////////////////////////
        // on the server, Spawn() spawns an Entity on all clients.
        // on the client, Spawn() reacts to the message and spawns it.
        public void Spawn(Bytes16 prefabId, ulong netId, bool owned, float3 position, quaternion rotation)
        {
            // find prefab to instantiate
            if (prefabSystem.Get(prefabId, out Entity prefab))
            {
                // instantiate the prefab
                Entity entity = EntityManager.Instantiate(prefab);

                // set prefabId once when spawning
                NetworkEntity networkEntity = GetComponent<NetworkEntity>(entity);
                networkEntity.prefabId = prefabId;
                SetComponent(entity, networkEntity);

                // set spawn position & rotation
                SetComponent(entity, new Translation{Value = position});
                SetComponent(entity, new Rotation{Value = rotation});

                // add to spawned before calling ApplyState
                spawned[netId] = entity;

                // reuse ApplyState to apply the rest of the state
                ApplyState(netId, owned);
                //Debug.LogWarning("NetworkClientSystem.Spawn: spawned " + EntityManager.GetName(entity) + " with prefabId=" + prefabId + " netId=" + netId);
            }
            else Debug.LogWarning("NetworkClientSystem.Spawn: unknown prefabId=" + Conversion.Bytes16ToGuid(prefabId));
        }

        // on the server, Unspawn() unspawns an Entity on all clients.
        // on the client, Unspawn() reacts to the message and unspawns it.
        public void Unspawn(ulong netId)
        {
            // find spawned entity
            if (spawned.TryGetValue(netId, out Entity entity))
            {
                // destroy the entity
                //Debug.LogWarning("NetworkClientSystem.Unspawn: unspawning " + EntityManager.GetName(entity) + " with netId=" + netId);
                EntityManager.DestroyEntity(entity);

                // remove from spawned
                spawned.Remove(netId);
            }
            else Debug.LogWarning("NetworkClientSystem.Unspawn: unknown netId=" + netId + ". Was unspawn called twice for the same netId?");
        }

        // synchronize an entity's state from a server message
        public void ApplyState(ulong netId, bool owned)
        {
            // find spawned entity
            if (spawned.TryGetValue(netId, out Entity entity))
            {
                // apply NetworkEntity data
                NetworkEntity networkEntity = GetComponent<NetworkEntity>(entity);
                networkEntity.netId = netId;
                networkEntity.owned = owned;
                SetComponent(entity, networkEntity);
            }
            else Debug.LogWarning("NetworkClientSystem.ApplyState: unknown netId=" + netId + ". Was the Entity spawned?");
        }

        // destroy all NetworkEntities to clean up
        protected void DestroyAllNetworkEntities()
        {
            // the query does NOT include prefabs
            EntityQuery networkEntities = GetEntityQuery(typeof(NetworkEntity));
            EntityManager.DestroyEntity(networkEntities);
        }

        // component system ////////////////////////////////////////////////////
        // cache TransportSystem in OnStartRunning after all systems were created
        // (we can't assume that TransportSystem.OnCreate is created before this)
        protected override void OnStartRunning()
        {
            // find available client transport
            transport = TransportSystem.FindAvailable(World) as TransportClientSystem;
            if (transport != null)
            {
                // hook ourselves up to Transport events
                transport.OnConnected = OnTransportConnected;
                transport.OnData = OnTransportData;
                transport.OnDisconnected = OnTransportDisconnected;

                // initialize send buffer
                sendBuffer = new byte[transport.GetMaxPacketSize()];
            }
            else Debug.LogError("NetworkClientSystem: no available TransportClientSystem found on this platform: " + Application.platform);
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            // disconnect client in case it was running
            Disconnect();
        }
    }
}
