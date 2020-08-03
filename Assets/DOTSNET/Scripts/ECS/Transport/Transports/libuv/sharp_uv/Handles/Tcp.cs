// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetUV.Core.Logging;
using NetUV.Core.Requests;

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetUV.Core.Native;

    public sealed class Tcp : ServerStream
    {
        internal Tcp(LoopContext loop)
            : base(loop, uv_handle_type.UV_TCP)
        { }

        public int GetSendBufferSize() => this.SendBufferSize(0);

        public int SetSendBufferSize(int value)
        {
            Contract.Requires(value > 0);

            return this.SendBufferSize(value);
        }

        public int GetReceiveBufferSize() => this.ReceiveBufferSize(0);


        public int SetReceiveBufferSize(int value)
        {
            Contract.Requires(value > 0);

            return this.ReceiveBufferSize(value);
        }

        public Tcp Bind(IPEndPoint endPoint, bool dualStack = false)
        {
            Contract.Requires(endPoint != null);

            this.Validate();
            NativeMethods.TcpBind(this.InternalHandle, endPoint, dualStack);

            return this;
        }

        public IPEndPoint GetLocalEndPoint()
        {
            this.Validate();
            return NativeMethods.TcpGetSocketName(this.InternalHandle);
        }

        public IPEndPoint GetPeerEndPoint()
        {
            this.Validate();
            return NativeMethods.TcpGetPeerName(this.InternalHandle);
        }

        public Tcp NoDelay(bool value)
        {
            this.Validate();
            NativeMethods.TcpSetNoDelay(this.InternalHandle, value);

            return this;
        }

        public Tcp KeepAlive(bool value, int delay)
        {
            this.Validate();
            NativeMethods.TcpSetKeepAlive(this.InternalHandle, value, delay);

            return this;
        }

        public Tcp SimultaneousAccepts(bool value)
        {
            this.Validate();
            NativeMethods.TcpSimultaneousAccepts(this.InternalHandle, value);

            return this;
        }

        protected internal override unsafe StreamHandle NewStream()
        {
            IntPtr loopHandle = ((uv_stream_t*)this.InternalHandle)->loop;
            LoopContext loop = HandleContext.GetTarget<LoopContext>(loopHandle);

            Tcp client = new Tcp(loop);
            NativeMethods.StreamAccept(this.InternalHandle, client.InternalHandle);
            client.ReadStart();

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} client {2} accepted",
                    this.HandleType, this.InternalHandle, client.InternalHandle);
            }

            return client;
        }

        public Tcp Listen(Action<Tcp, Exception> onConnection, int backlog = DefaultBacklog)
        {
            Contract.Requires(onConnection != null);
            Contract.Requires(backlog > 0);

            this.StreamListen((handle, exception) => onConnection((Tcp)handle, exception), backlog);
            return this;
        }

        public Tcp Listen(IPEndPoint localEndPoint,
                          Action<Tcp, Exception> onConnection,
                          int backlog = ServerStream.DefaultBacklog,
                          bool dualStack = false)
        {
            Contract.Requires(localEndPoint != null);
            Contract.Requires(onConnection != null);

            Bind(localEndPoint, dualStack);
            Listen(onConnection, backlog);
            return this;
        }

        public Tcp ConnectTo(IPEndPoint localEndPoint,
                             IPEndPoint remoteEndPoint,
                             Action<Tcp, Exception> connectedAction,
                             bool dualStack = false)
        {
            Contract.Requires(localEndPoint != null);
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(connectedAction != null);

            Bind(localEndPoint, dualStack);
            ConnectTo(remoteEndPoint, connectedAction);
            return this;
        }

        public Tcp ConnectTo(IPEndPoint remoteEndPoint,
                             Action<Tcp, Exception> connectedAction,
                             bool dualStack = false)
        {
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(connectedAction != null);

            TcpConnect request = null;
            try
            {
                request = new TcpConnect(this, remoteEndPoint, connectedAction);
            }
            catch (Exception)
            {
                request?.Dispose();
                throw;
            }

            return this;
        }

        /*public Tcp Bind(IPEndPoint localEndPoint,
                        Action<StreamHandle, IStreamReadCompletion> onRead,
                        bool dualStack = false)
        {
            Contract.Requires(localEndPoint != null);
            Contract.Requires(onRead != null);

            Bind(localEndPoint, dualStack);
            OnRead(onRead);
            return this;
        }*/
    }
}
