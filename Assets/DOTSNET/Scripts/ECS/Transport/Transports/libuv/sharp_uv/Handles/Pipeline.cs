// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoProperty
namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Logging;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    sealed class Pipeline : IDisposable
    {
        readonly StreamHandle streamHandle;
        // PendingRead keeps track of the current read buffer and pins it.
        // libuv Allocate+Read callbacks always go in pairs:
        //   https://github.com/libuv/libuv/issues/1085
        //   https://libuv.narkive.com/cn1gwvvA/passing-a-small-buffer-to-alloc-cb-results-in-multiple-calls-to-alloc-cb
        // So PendingRead allocates the buffer only once, and then reuses it.
        // It also pins it so that GC doesn't move it while libuv is reading.
        readonly PendingRead pendingRead;
        StreamConsumer<StreamHandle> streamConsumer;

        // configurable buffer sizes.
        // make sure to modify them before starting.
        public static int ReceiveBufferSize = 65536;
        public static int SendBufferSize = 65536;

        internal Pipeline(StreamHandle streamHandle)
        {
            Debug.Assert(streamHandle != null);

            this.streamHandle = streamHandle;
            pendingRead = new PendingRead();
        }

        internal void Consumer(StreamConsumer<StreamHandle> consumer)
        {
            Debug.Assert(consumer != null);
            streamConsumer = consumer;
        }

        // called by libuv when it needs to allocate for reading
        internal uv_buf_t AllocateReadBuffer()
        {
            // get pendingRead's internal buffer which is only allocated once.
            return pendingRead.GetBuffer(ReceiveBufferSize);
        }

        internal UnsafeReadBuffer GetReadBuffer()
        {
            UnsafeReadBuffer byteBuffer = pendingRead.Buffer;
            return byteBuffer;
        }

        internal void OnReadCompleted(UnsafeReadBuffer byteBuffer, Exception error)
        {
            InvokeRead(byteBuffer, 0, error, true);
        }

        internal void OnReadCompleted(UnsafeReadBuffer byteBuffer, int size)
        {
            InvokeRead(byteBuffer, size);
        }

        void InvokeRead(UnsafeReadBuffer byteBuffer, int size, Exception error = null, bool completed = false)
        {
            // note: no size > 0 check because for dis/connect etc. size is 0.

            // set amount of readable bytes in byteBuffer
            // TODO make this more simple
            byteBuffer.SetWriterIndex(byteBuffer.WriterIndex + size);

            try
            {
                streamConsumer?.Consume(streamHandle, byteBuffer,  error, completed);
            }
            catch (Exception exception)
            {
                Log.Warn($"{nameof(Pipeline)} Exception whilst invoking read callback.", exception);
            }

            // note: we don't free the buffer in finally() because pendingRead
            //       holds on to it to avoid allocations
        }

        internal void QueueWrite(ArraySegment<byte> segment, Action<StreamHandle, Exception> completion)
        {
            WriteRequest request = Loop.WriteRequestPool.Take();
            try
            {
                // prepare request with our completion callback, and make sure that
                // streamHandle is passed as first parameter.
                request.Prepare(segment, completion, streamHandle);
                streamHandle.WriteStream(request);
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(Pipeline)} {streamHandle.HandleType} faulted.", exception);
                request.Release();
                throw;
            }
        }

        public void Dispose()
        {
            pendingRead.Dispose();
            streamConsumer = null;
        }
    }
}
