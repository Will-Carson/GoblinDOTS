// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace NetUV.Core.Native
{
    using System;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;

    sealed class PendingRead : IDisposable
    {
        UnsafeReadBuffer buffer;
        GCHandle pin;

        internal UnsafeReadBuffer Buffer => this.buffer;

        internal uv_buf_t GetBuffer(int BufferSize)
        {
            // create buffer if not created yet
            if (buffer == null)
            {
                buffer = new UnsafeReadBuffer(BufferSize);

            }
            // if created yet, then we don't want to reallocate it.
            // make sure that buffer size is same
            if (buffer.Capacity != BufferSize)
                throw new ArgumentException("GetBuffer always should be called with same buffer size to avoid allcoations!");

            // pin it so GC doesn't dispose or move it while libuv is using
            // it.
            //
            // the assert is extremely slow, and it allocates.
            //Debug.Assert(!this.pin.IsAllocated);
            IntPtr arrayHandle = buffer.AddressOfPinnedMemory();
            int index = buffer.WriterIndex;
            if (arrayHandle == IntPtr.Zero)
            {
                pin = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                arrayHandle = this.pin.AddrOfPinnedObject();
                index += buffer.ArrayOffset;
            }
            int length = buffer.WritableBytes;
            return new uv_buf_t(arrayHandle + index, length);
        }

        void Release()
        {
            if (this.pin.IsAllocated)
            {
                this.pin.Free();
            }
        }

        public void Dispose()
        {
            this.Release();
        }
    }
}
