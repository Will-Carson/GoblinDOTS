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
        // Apathy default MaxSize is 16 KB. let's do 64 because we combine
        // messages.
        public int MaxMessageSize = 64 * 1024;
        // for large ECS worlds, 100/tick is not enough:
        public int MaxReceivesPerTickPerConnection = 1000;

        // server
        internal Server server = new Server();

        // cache GetNextMessages queue to avoid allocations
        // -> with capacity to avoid rescaling as long as possible!
        Queue<Message> queue = new Queue<Message>(1000);

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

        public override int GetMaxPacketSize() => MaxMessageSize;
        public override bool IsActive() => server.Active;
        public override void Start() => server.Start(Port);
        public override bool Send(int connectionId, ArraySegment<byte> segment)
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
                        else Debug.LogError("ApathyTransportServerSystem: failed to send segment of size " + segment.Count + " to connectionId=" + connectionId + " because it's bigger than MaxMessageSize=" + MaxMessageSize);
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
            server.MaxMessageSize = MaxMessageSize;
            server.MaxReceivesPerTickPerConnection = MaxReceivesPerTickPerConnection;
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

        void ProcessIncomingMessages()
        {
            server.GetNextMessages(queue);
            while (queue.Count > 0)
            {
                Message message = queue.Dequeue();
                switch (message.eventType)
                {
                    case EventType.Connected:
                        sendQueue[message.connectionId] = new SegmentWriterWrapped(new SegmentWriter(new byte[MaxMessageSize]));
                        OnConnected(message.connectionId);
                        break; // breaks switch, not while
                    case EventType.Data:
                        OnData(message.connectionId, message.data);
                        break; // breaks switch, not while
                    case EventType.Disconnected:
                        OnDisconnected(message.connectionId);
                        sendQueue.Remove(message.connectionId);
                        break; // breaks switch, not while
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
                ProcessIncomingMessages();
            }
        }

        protected override void OnDestroy()
        {
            Debug.Log("ApathyTransportServerSystem shutdown!");
            server.Stop();
        }
    }
}
