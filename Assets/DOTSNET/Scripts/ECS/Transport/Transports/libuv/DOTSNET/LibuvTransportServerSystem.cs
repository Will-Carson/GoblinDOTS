// see also: https://github.com/StormHub/NetUV/blob/dev/examples/EchoServer/TcpServer.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Unity.Entities;
using UnityEngine;
using NetUV.Core.Buffers;
using NetUV.Core.Handles;

namespace DOTSNET.Libuv
{
    class Connection
    {
        public Tcp client;
        public SegmentWriter incoming = new SegmentWriter(new byte[LibuvTransportServerSystem.SendReceiveBufferSize]);
        public Connection(Tcp client) { this.client = client; }
    }

    [ServerWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class LibuvTransportServerSystem : TransportServerSystem
    {
        // configuration
        public ushort Port = 7777;
        // we need a big send/recv buffer for DOTSNET
        public const int SendReceiveBufferSize = 10 * 1024 * 1024;

        // Libuv state
        Loop loop = new Loop();
        Tcp server;
        Dictionary<int, Connection> connections = new Dictionary<int, Connection>();
        int mainThreadId;

        // -> payload: MaxMessageSize + header size
        //    (size is const so it's ok to create it once without rescaling)
        protected byte[] payloadBuffer = new byte[SendReceiveBufferSize];

        public override bool Available()
        {
            // available only where we built the native library
            return Application.platform == RuntimePlatform.OSXEditor ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.LinuxEditor ||
                   Application.platform == RuntimePlatform.LinuxPlayer;
        }

        // DOTSNET should send up to buffersize-4 so we still have space to add the header
        public override int GetMaxPacketSize() => SendReceiveBufferSize - 4;

        public override bool IsActive() => server != null;

        public override void Start()
        {
            if (server != null)
                return;

            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            // start server
            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Any, Port);

            Debug.Log($"libuv sv: starting TCP..." + EndPoint);
            server = loop
                .CreateTcp()
                .SimultaneousAccepts(true)
                .Listen(EndPoint, OnLibuvConnected);
            Debug.Log($"libuv sv: TCP started!");
        }

        // note: DOTSNET already packs messages. Transports don't need to.
        public override bool Send(int connectionId, ArraySegment<byte> segment, Channel channel)
        {
            if (server != null && connections.TryGetValue(connectionId, out Connection connection))
            {
                // create <size, data> payload so we only call write once.
                LibuvUtils.ConstructPayload(payloadBuffer, segment);

                // send it
                ArraySegment<byte> payload = new ArraySegment<byte>(payloadBuffer, 0, segment.Count + 4);
                connection.client.WriteStream(payload, OnLibuvWriteCompleted);
                return true;
            }
            return false;
        }

        public override bool Disconnect(int connectionId)
        {
            if (server != null && connections.TryGetValue(connectionId, out Connection connection))
            {
                connection.client.Dispose();
                return true;
            }
            return false;
        }

        public override string GetAddress(int connectionId)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            if (server != null)
            {
                server.Dispose();
                server = null;
                connections.Clear();
            }
        }

        // libuv callbacks /////////////////////////////////////////////////////

        // TODO this might not be thread safe
        void OnLibuvConnected(Tcp client, Exception error)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                Debug.LogError("Libuv callback not thread safe.");
                return;
            }

            if (error != null)
            {
                Debug.Log($"libuv sv: client connection failed {error}");
                client.CloseHandle(OnLibuvClosed);
                return;
            }

            connections[client.InternalHandle.ToInt32()] = new Connection(client);

            // dotsnet event
            base.OnConnected(client.InternalHandle.ToInt32());

            client.OnRead(this.OnLibuvData, OnLibuvError);
        }

        // TODO this might not be thread safe
        byte[] buffer = new byte[SendReceiveBufferSize];
        void OnLibuvData(StreamHandle handle, UnsafeReadBuffer data)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                Debug.LogError("Libuv callback not thread safe.");
                return;
            }

            if (data.ReadableBytes == 0)
            {
                return;
            }

            // find connection
            if (connections.TryGetValue(handle.InternalHandle.ToInt32(), out Connection connection))
            {
                // copy all the new data allocation free into our buffer
                if (data.ReadableBytes > buffer.Length)
                {
                    Debug.LogError("Libuv client buffer too small for: " + data.ReadableBytes);
                    return;
                }
                int count = data.ReadableBytes;
                data.ReadBytes(buffer, 0, data.ReadableBytes);

                // write into incoming buffer
                if (!connection.incoming.WriteBytes(new ArraySegment<byte>(buffer, 0, count)))
                {
                    Debug.LogError("Libuv client incoming is full!");
                    handle.Dispose();
                    return;
                }

                // ...

                // read as many messages we can
                while (true)
                {
                    SegmentReader reader = new SegmentReader(connection.incoming.segment);
                    // read 4 bytes header
                    if (reader.ReadBytes(4, out ArraySegment<byte> header))
                    {
                        // read network order for compatibility with erlang/apathy/telepathy/etc.
                        int size = LibuvUtils.BytesToIntBigEndian(header.Array, header.Offset);
                        if (reader.Remaining >= size)
                        {
                            // read data
                            if (reader.ReadBytes(size, out ArraySegment<byte> segment))
                            {
                                //Debug.Log("Libuv cl process segment: " + segment.Count + ", remainder=" + reader.Remaining);

                                // DOTSNET event
                                base.OnData(handle.InternalHandle.ToInt32(), segment);

                                // cut out the message from our incoming reader by
                                // copying everything after the message to the front
                                reader.ReadBytes(reader.Remaining, out ArraySegment<byte> remainder);
                                connection.incoming.Position = 0;
                                connection.incoming.WriteBytes(remainder);

                                // while loop will try to read the remainder
                            }
                            else
                            {
                                Debug.LogError("Reader readbytes failed even though it should never.");
                                break;
                            }
                        }
                        else break;
                    }
                    else break;
                }
            }
            else Debug.LogError("libuv sv: invalid connectionid: " + handle.InternalHandle.ToInt32());
        }

        void OnLibuvWriteCompleted(StreamHandle handle, Exception error)
        {
            if (error != null)
            {
                Debug.LogWarning($"libuv cl: write error {error}");
            }
        }

        // TODO this might not be thread safe
        void OnLibuvError(StreamHandle handle, Exception error)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                Debug.LogError("Libuv callback not thread safe.");
                return;
            }

            Debug.Log($"libuv sv: error {error}");
            connections.Remove(handle.InternalHandle.ToInt32());
            handle.CloseHandle(OnLibuvClosed);
        }

        // TODO this might not be thread safe
        void OnLibuvClosed(StreamHandle handle)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                Debug.LogError("Libuv callback not thread safe.");
                return;
            }

            Debug.Log($"libuv sv: closed client {handle}");

            // dotsnet event
            base.OnDisconnected(handle.InternalHandle.ToInt32());

            connections.Remove(handle.InternalHandle.ToInt32());
            handle.Dispose();
        }

        // ECS /////////////////////////////////////////////////////////////////
        protected override void OnStartRunning()
        {
            // configure buffer sizes
            Pipeline.ReceiveBufferSize = Pipeline.SendBufferSize = SendReceiveBufferSize;
        }

        protected override void OnUpdate()
        {
            // tick once
            if (loop != null && server != null)
            {
                for (int i = 0; i < 100; ++i)
                    loop.RunNoWait();
            }
        }
    }
}