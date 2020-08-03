using System;
using System.Net;
using System.Threading;
using NetUV.Core.Buffers;
using NetUV.Core.Handles;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET.Libuv
{
    [ClientWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class LibuvTransportClientSystem : TransportClientSystem
    {
        // configuration
        public ushort Port = 7777;
        // we need a big send/recv buffer for DOTSNET
        public const int SendReceiveBufferSize = 10 * 1024 * 1024;

        // Libuv state
        Loop loop = new Loop();
        Tcp client;
        int mainThreadId;
        SegmentWriter incoming = new SegmentWriter(new byte[SendReceiveBufferSize]);

        // payload buffer: MaxMessageSize + header size
        // (size is const so it's ok to create it once without rescaling)
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

        public override bool IsConnected() =>
            client != null && client.IsActive;

        public override void Connect(string hostname)
        {
            if (client != null)
                return;

            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            // libuv doesn't resolve host name, and it needs ipv4.
            if (LibuvUtils.ResolveToIPV4(hostname, out IPAddress address))
            {
                // connect client
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                IPEndPoint remoteEndPoint = new IPEndPoint(address, Port);

                Debug.Log("Libuv connecting to: " + address + ":" + Port);
                client = loop
                    .CreateTcp()
                    .NoDelay(true)
                    .ConnectTo(localEndPoint, remoteEndPoint, OnLibuvConnected);
            }
            else Debug.LogWarning("Libuv Connect: no IPv4 found for hostname: " + hostname);
        }

        public override bool Send(ArraySegment<byte> segment, Channel channel)
        {
            if (loop != null && client != null)
            {
                // create <size, data> payload so we only call write once.
                LibuvUtils.ConstructPayload(payloadBuffer, segment);

                ArraySegment<byte> payload = new ArraySegment<byte>(payloadBuffer, 0, segment.Count + 4);
                // sharp_uv copies segment internally.
                client.WriteStream(payload, OnLibuvWriteCompleted);
                return true;
            }
            return false;
        }

        public override void Disconnect()
        {
            if (client != null)
            {
                base.OnDisconnected();
                client.Dispose();
                client = null;
            }
        }

        // libuv callbacks /////////////////////////////////////////////////////
        // TODO this might not be thread safe
        void OnLibuvConnected(Tcp client, Exception exception)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                Debug.LogError("Libuv callback not thread safe.");
                return;
            }

            if (exception != null)
            {
                Debug.Log($"libuv cl: client error {exception}");
                client.CloseHandle(OnLibuvClosed);
                return;
            }

            // dotsnet event
            base.OnConnected();

            Debug.Log($"libuv cl: client connected.");
            client.OnRead(OnLibuvData, OnLibuvError);

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

            // copy all the new data allocation free into our buffer
            if (data.ReadableBytes > buffer.Length)
            {
                Debug.LogError("Libuv client buffer too small for: " + data.ReadableBytes);
                return;
            }
            int count = data.ReadableBytes;
            data.ReadBytes(buffer, 0, data.ReadableBytes);

            // write into incoming buffer
            if (!incoming.WriteBytes(new ArraySegment<byte>(buffer, 0, count)))
            {
                Debug.LogError("Libuv client incoming is full!");
                handle.Dispose();
                return;
            }

            // ...

            // read as many messages we can
            while (true)
            {
                SegmentReader reader = new SegmentReader(incoming.segment);
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
                            base.OnData(segment);

                            // cut out the message from our incoming reader by
                            // copying everything after the message to the front
                            reader.ReadBytes(reader.Remaining, out ArraySegment<byte> remainder);
                            incoming.Position = 0;
                            incoming.WriteBytes(remainder);

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

            Debug.LogWarning($"libuv cl: read error {error}");
            handle.CloseHandle(OnLibuvClosed);

            // dotsnet event
            base.OnDisconnected();
        }

        // TODO this might not be thread safe
        void OnLibuvClosed(StreamHandle handle)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                Debug.LogError("Libuv callback not thread safe.");
                return;
            }

            Debug.Log("libuv cl: closed connection");
            handle.Dispose();

            // dotsnet event
            base.OnDisconnected();
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
            if (loop != null && client != null)
            {
                for (int i = 0; i < 100; ++i)
                    loop.RunNoWait();
            }
        }
    }
}