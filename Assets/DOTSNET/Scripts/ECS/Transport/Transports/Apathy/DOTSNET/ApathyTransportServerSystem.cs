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
        // NoDelay disabled by default because DOTS will have large amounts of
        // messages and TCP's internal send interval buffering might be helpful.
        public bool NoDelay = false;
        // for large ECS worlds, 100/tick is not enough:
        public int MaxReceivesPerTickPerConnection = 1000;

        // server
        internal Server server = new Server();

        // queue messages to be sent out in OnUpdate
        // -> 20x faster than sending each messages separately
        // -> fewer native C calls
        // -> significantly less bandwidth because less TCP packets
        // -> callers spend 0 time calling Send(). we can see the true Send()
        //    workload from this system in the Entity debugger.
        //
        // SegmentWriter's array is MaxMessageSize bytes long because that's how
        // much our Socket buffers can handle at once. Most messages are
        // significantly smaller than that.
        Dictionary<int, SegmentWriterWrapped> sendQueue = new Dictionary<int, SegmentWriterWrapped>();

        // wrap SegmentWriter in a class so we can modify it inside the foreach
        class SegmentWriterWrapped
        {
            public SegmentWriter writer;
            public SegmentWriterWrapped(SegmentWriter writer) =>
                this.writer = writer;
        }

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
        public override bool Send(int connectionId, ArraySegment<byte> segment, Channel channel)
        {
            // write message to send queue and send it in OnUpdate later
            if (sendQueue.TryGetValue(connectionId, out SegmentWriterWrapped wrapper))
            {
                // add to connection's send queue if it has enough space
                // -> SegmentWriter is atomic, so it doesn't write anything
                //    unless it has enough space
                // (we wrapped writer in a class, so no need to reassign)
                if (wrapper.writer.WriteBytesAndSize(segment))
                {
                    return true;
                }
                // otherwise the send queue is too full.
                else
                {
                    // flush the queue immediately. we can't wait until OnUpdate
                    // to send it, because even in OnUpdate we can only send up
                    // to MaxMessageSize at once.
                    if (FlushQueue(connectionId, wrapper))
                    {
                        // try to add to queue again
                        if (wrapper.writer.WriteBytesAndSize(segment))
                        {
                            return true;
                        }
                        // it can return false if segment > MaxMessageSize
                        else Debug.LogError("ApathyTransportServerSystem: failed to send segment of size " + segment.Count + " to connectionId=" + connectionId + " because it's bigger than MaxMessageSize=" + Common.MaxMessageSize);
                    }
                }
            }
            return false;
        }
        public override bool Disconnect(int connectionId) => server.Disconnect(connectionId);
        public override string GetAddress(int connectionId) => server.GetClientAddress(connectionId);
        public override void Stop()
        {
            server.Stop();
            sendQueue.Clear();
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
                SegmentWriter writer = new SegmentWriter(new byte[Common.MaxMessageSize]);
                sendQueue[connectionId] = new SegmentWriterWrapped(writer);
                OnConnected(connectionId);
            };
            server.OnData = OnData;
            server.OnDisconnected = (connectionId) =>
            {
                OnDisconnected(connectionId);
                sendQueue.Remove(connectionId);
            };

            Debug.Log("ApathyTransportServerSystem initialized!");
        }

        bool FlushQueue(int connectionId, SegmentWriterWrapped wrapper)
        {
            // send the whole queue at once
            if (server.Send(connectionId, wrapper.writer.segment))
            {
                // reset position
                wrapper.writer.Position = 0;
                return true;
            }
            return false;
        }

        void ProcessOutgoingMessages()
        {
            foreach (KeyValuePair<int, SegmentWriterWrapped> kvp in sendQueue)
            {
                // any messages queued for this connectionId?
                if (kvp.Value.writer.Position > 0)
                {
                    // send the whole thing and reset position
                    // (we wrapped it in a class, so we can modify writer
                    //  from within our loop here)
                    FlushQueue(kvp.Key, kvp.Value);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (server.Active)
            {
                // send queued messages before processing incoming messages.
                // -> we need to flush the queue in each OnUpdate for situations
                //    where we Send() a small message and then nothing for a
                //    long time. we can't wait until send queue is full to flush
                //    them, it could take minutes. so we do it each OnUpdate.
                // -> sending only each 'interval' would be pointless because
                //    under heavy load (e.g. 10k demo), Send() flushes a full
                //    queue so often that a send interval would be meaningless.
                ProcessOutgoingMessages();

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
