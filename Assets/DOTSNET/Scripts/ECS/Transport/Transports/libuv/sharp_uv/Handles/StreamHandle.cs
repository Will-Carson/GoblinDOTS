// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using NetUV.Core.Logging;

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    public abstract class StreamHandle : ScheduleHandle
    {
        internal static readonly uv_alloc_cb AllocateCallback = OnAllocateCallback;
        internal static readonly uv_read_cb ReadCallback = OnReadCallback;

        readonly Pipeline pipeline;

        internal StreamHandle(
            LoopContext loop,
            uv_handle_type handleType,
            params object[] args)
            : base(loop, handleType, args)
        {
            this.pipeline = new Pipeline(this);
        }

        public bool IsReadable => NativeMethods.IsStreamReadable(this.InternalHandle);

        public bool IsWritable => NativeMethods.IsStreamWritable(this.InternalHandle);

        protected int SendBufferSize(int value)
        {
            Contract.Requires(value >= 0);

            this.Validate();
            return NativeMethods.SendBufferSize(this.InternalHandle, value);
        }

        protected int ReceiveBufferSize(int value)
        {
            Contract.Requires(value >= 0);

            this.Validate();
            return NativeMethods.ReceiveBufferSize(this.InternalHandle, value);
        }

        public void OnRead(
            Action<StreamHandle, UnsafeReadBuffer> onAccept,
            Action<StreamHandle, Exception> onError,
            Action<StreamHandle> onCompleted = null)
        {
            Contract.Requires(onAccept != null);
            Contract.Requires(onError != null);

            StreamConsumer<StreamHandle> consumer = new StreamConsumer<StreamHandle>(onAccept, onError, onCompleted);
            this.pipeline.Consumer(consumer);
        }

        public void Shutdown(Action<StreamHandle, Exception> completion = null)
        {
            if (!this.IsValid)
            {
                return;
            }

            StreamShutdown streamShutdown = null;
            try
            {
                streamShutdown = new StreamShutdown(this, completion);
            }
            catch (Exception exception)
            {
                Exception error = exception;

                ErrorCode? errorCode = (error as OperationException)?.ErrorCode;
                if (errorCode == ErrorCode.EPIPE)
                {
                    // It is ok if the stream is already down
                    error = null;
                }
                if (error != null)
                {
                    Log.Error($"{this.HandleType} {this.InternalHandle} failed to shutdown.", error);
                }

                StreamShutdown.Completed(completion, this, error);
                streamShutdown?.Dispose();
            }
        }

        public void CloseHandle(Action<StreamHandle> callback = null)
        {
            Action<ScheduleHandle> handler = null;
            if (callback != null)
            {
                handler = state => callback((StreamHandle)state);
            }

            base.CloseHandle(handler);
        }

        // segment data is copied internally and can be reused immediately.
        public void WriteStream(ArraySegment<byte> segment,
            Action<StreamHandle, Exception> completion)
        {
            this.pipeline.QueueWrite(segment, completion);
        }

        // WriteStream writes the final WriteRequest
        internal unsafe void WriteStream(WriteRequest request)
        {
            Debug.Assert(request != null);

            this.Validate();
            try
            {
                NativeMethods.WriteStream(
                    request.InternalHandle,
                    this.InternalHandle,
                    request.Bufs,
                    ref request.Size);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} Failed to write data {request}.", exception);
                throw;
            }
        }

        internal void ReadStart()
        {
            this.Validate();
            NativeMethods.StreamReadStart(this.InternalHandle);
        }

        internal void ReadStop()
        {
            if (!this.IsValid)
            {
                return;
            }

            // This function is idempotent and may be safely called on a stopped stream.
            NativeMethods.StreamReadStop(this.InternalHandle);
        }

        protected override void Close() => this.pipeline.Dispose();

        void OnReadCallback(UnsafeReadBuffer byteBuffer, int status)
        {
            //
            //  nread is > 0 if there is data available or < 0 on error.
            //  When we’ve reached EOF, nread will be set to UV_EOF.
            //  When nread < 0, the buf parameter might not point to a valid buffer;
            //  in that case buf.len and buf.base are both set to 0
            //

            // For status = 0 (Nothing to read)
            if (status >= 0)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat("{0} {1} read, buffer length = {2} status = {3}.", this.HandleType, this.InternalHandle, byteBuffer.Capacity, status);
                }

                this.pipeline.OnReadCompleted(byteBuffer, status);
                return;
            }

            Exception exception = null;
            if (status != (int)uv_err_code.UV_EOF) // Stream end is not an error
            {
                exception = NativeMethods.CreateError((uv_err_code)status);
                Log.Error($"{this.HandleType} {this.InternalHandle} read error, status = {status}", exception);
            }
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} read completed.", this.HandleType, this.InternalHandle);
            }

            this.pipeline.OnReadCompleted(byteBuffer, exception);
            this.ReadStop();
        }

        static void OnReadCallback(IntPtr handle, IntPtr nread, ref uv_buf_t buf)
        {
            StreamHandle stream = HandleContext.GetTarget<StreamHandle>(handle);
            UnsafeReadBuffer byteBuffer = stream.pipeline.GetReadBuffer();
            stream.OnReadCallback(byteBuffer, (int)nread.ToInt64());
        }

        static void OnAllocateCallback(IntPtr handle, IntPtr suggestedSize, out uv_buf_t buf)
        {
            StreamHandle stream = HandleContext.GetTarget<StreamHandle>(handle);
            buf =  stream.pipeline.AllocateReadBuffer();
        }
    }
}
