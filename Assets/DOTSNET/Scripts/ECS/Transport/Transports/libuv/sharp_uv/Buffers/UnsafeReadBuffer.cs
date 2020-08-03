// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoProperty
using System;
using System.Runtime.CompilerServices;
//using System.Threading;
using NetUV.Core.Common;

namespace NetUV.Core.Buffers
{
    // reader for unsafe memory buffer
    public sealed class UnsafeReadBuffer
    {
        int readerIndex;
        int writerIndex;
        int capacity;
        byte[] buffer;

        public int WriterIndex => this.writerIndex;

        public UnsafeReadBuffer(int capacity)
        {
            this.buffer = new byte[capacity];
            this.capacity = capacity;
        }

        public void SetWriterIndex(int index)
        {
            if (index < this.readerIndex || index > this.Capacity)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_WriterIndex(index, this.readerIndex, this.Capacity);
            }

            this.SetWriterIndex0(index);
        }

        void SetWriterIndex0(int index)
        {
            this.writerIndex = index;
        }

        public int ReadableBytes => this.writerIndex - this.readerIndex;

        public int WritableBytes => this.Capacity - this.writerIndex;

        public int Capacity => this.capacity;

        public bool HasArray => true;

        public byte[] Array
        {
            get
            {
                this.EnsureAccessible();
                return this.buffer;
            }
        }

        public int ArrayOffset => 0;

        public bool HasMemoryAddress => true;

        public ref byte GetPinnableMemoryAddress()
        {
            this.EnsureAccessible();
            return ref this.buffer[0];
        }

        public IntPtr AddressOfPinnedMemory() => IntPtr.Zero;

        public unsafe void GetBytes(int index, UnsafeReadBuffer dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.GetBytes(this, addr, index, dst, dstIndex, length);
        }

        public unsafe void GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.GetBytes(this, addr, index, dst, dstIndex, length);
        }

        public unsafe void SetBytes(int index, UnsafeReadBuffer src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetBytes(this, addr, index, src, srcIndex, length);
        }

        public void ReadBytes(byte[] destination, int dstIndex, int length)
        {
            this.CheckReadableBytes(length);
            this.GetBytes(this.readerIndex, destination, dstIndex, length);
            this.readerIndex += length;
        }

        internal void CheckIndex(int index, int fieldLength)
        {
            this.EnsureAccessible();
            this.CheckIndex0(index, fieldLength);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckIndex0(int index, int fieldLength)
        {
            if (MathUtil.IsOutOfBounds(index, fieldLength, this.Capacity))
            {
                ThrowHelper.ThrowIndexOutOfRangeException_Index(index, fieldLength, this.Capacity);
            }
        }

        void CheckReadableBytes(int minimumReadableBytes)
        {
            if (minimumReadableBytes < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_MinimumReadableBytes(minimumReadableBytes);
            }

            this.CheckReadableBytes0(minimumReadableBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckReadableBytes0(int minimumReadableBytes)
        {
            this.EnsureAccessible();
            if (this.readerIndex > this.writerIndex - minimumReadableBytes)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_ReaderIndex(minimumReadableBytes, this.readerIndex, this.writerIndex, this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureAccessible()
        {
            /*if (this.ReferenceCount == 0)
            {
                ThrowHelper.ThrowIllegalReferenceCountException(0);
            }*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ref byte Addr(int index) => ref this.buffer[index];

        // reference counting //////////////////////////////////////////////////
        // libuv requires reference counting for buffers.
        // see the uv book.

        // not needed at the moment because we keep one buffer at all times in
        // pendingRead.
        /*volatile int referenceCount = 1;
        public int ReferenceCount => this.referenceCount;

        public bool Release() => this.Release0(1);

        bool Release0(int decrement)
        {
            while (true)
            {
                int refCnt = this.ReferenceCount;
                if (refCnt < decrement)
                {
                    ThrowHelper.ThrowIllegalReferenceCountException(refCnt, -decrement);
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, refCnt - decrement, refCnt) == refCnt)
                {
                    if (refCnt == decrement)
                    {
                        this.Deallocate();
                        return true;
                    }

                    return false;
                }
            }
        }

        internal void Deallocate()
        {
            UnityEngine.Debug.LogWarning("DEALLOCATE BUF");
            byte[] buf = this.buffer;
            if (buf == null)
            {
                return;
            }

            this.buffer = null;
        }*/
    }
}
