// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace NetUV.Core.Requests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using NetUV.Core.Handles;
    using NetUV.Core.Buffers;
    using NetUV.Core.Common;
    using NetUV.Core.Native;

    sealed class WriteRequest : ScheduleRequest
    {
        const int MaximumLimit = 32;

        internal static readonly uv_watcher_cb WriteCallback = OnWriteCallback;
        static readonly int BufferSize;

        // reference to the pool to know where to return to.
        // -> we avoid static state for Unity's new DOTS 'disable domain reload'
        readonly ConcurrentPool<WriteRequest> recycler;

        readonly RequestContext requestContext;
        readonly List<GCHandle> handles;

        IntPtr bufs;
        GCHandle pin;
        int count;

        uv_buf_t[] bufsArray;

        // Prepare() gets an ArraySegment, but we can't assume that it persists
        // until the completed callback. so we need to copy into an internal
        // buffer.
        // we use Pipeline.SendBufferSize for every WriteRequest.
        // this way we avoid allocations since WriteRequest itself is pooled!
        byte[] data = new byte[Pipeline.SendBufferSize];

        Action<StreamHandle, Exception> completion;
        StreamHandle completionHandle; // first parameter for completion

        static WriteRequest()
        {
            BufferSize = Marshal.SizeOf<uv_buf_t>();
        }

        internal WriteRequest(uv_req_type requestType, ConcurrentPool<WriteRequest> recycler)
            : base(requestType)
        {
            Debug.Assert(requestType == uv_req_type.UV_WRITE || requestType == uv_req_type.UV_UDP_SEND);

            requestContext = new RequestContext(requestType, BufferSize * MaximumLimit, this);
            this.recycler = recycler;
            handles = new List<GCHandle>();

            IntPtr addr = requestContext.Handle;
            bufs = addr + requestContext.HandleSize;
            pin = GCHandle.Alloc(addr, GCHandleType.Pinned);
            count = 0;
        }

        internal override IntPtr InternalHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => requestContext.Handle;
        }

        internal unsafe void Prepare(ArraySegment<byte> segment, Action<StreamHandle, Exception> callback, StreamHandle callbackHandle)
        {
            if (!requestContext.IsValid)
            {
                ThrowHelper.ThrowInvalidOperationException_WriteRequest();
            }

            completion = callback;
            completionHandle = callbackHandle;

            // we can't assume that the ArraySegment that we passed in Send()
            // will persist until OnCompleted (in Mirror/DOTSNET it won't).
            // so we need to copy it to our internal data buffer.
            if (segment.Count > data.Length)
            {
                throw new ArgumentException("Segment.Count=" + segment.Count + " is too big for fixed internal buffer size=" + data.Length);
            }
            fixed (byte* buf = data)
            {
                PlatformDependent.CopyMemory(segment.Array, segment.Offset, buf, segment.Count);
            }

            // now pin the data array
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            handles.Add(handle);
            IntPtr addr = handle.AddrOfPinnedObject();
            Add(addr, 0, segment.Count);
        }

        void Add(IntPtr addr, int offset, int len)
        {
            IntPtr baseOffset = bufs + BufferSize * count;
            ++count;
            uv_buf_t.InitMemory(baseOffset, addr + offset, len);
        }

        internal unsafe uv_buf_t* Bufs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => bufsArray == null ? (uv_buf_t*)bufs : (uv_buf_t*)Unsafe.AsPointer(ref bufsArray[0]);
        }

        internal ref int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref count;
        }

        internal void Release()
        {
            if (handles.Count > 0)
            {
                for (int i = 0; i < handles.Count; i++)
                {
                    if (handles[i].IsAllocated)
                    {
                        handles[i].Free();
                    }
                }
                handles.Clear();
            }

            bufsArray = null;
            completion = null;
            count = 0;
            recycler.Return(this);
        }

        void Free()
        {
            Release();
            if (pin.IsAllocated)
            {
                pin.Free();
            }
            bufs = IntPtr.Zero;
        }

        void OnWriteCallback(int status)
        {
            OperationException error = null;
            if (status < 0)
            {
                error = NativeMethods.CreateError((uv_err_code)status);
            }

            Release();
            completion?.Invoke(completionHandle, error);
        }

        static void OnWriteCallback(IntPtr handle, int status)
        {
            var request = RequestContext.GetTarget<WriteRequest>(handle);
            request.OnWriteCallback(status);
        }

        protected override void Close()
        {
            if (bufs != IntPtr.Zero)
            {
                Free();
            }
            requestContext.Dispose();
        }
    }
}
