// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming
namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics;
    using NetUV.Core.Common;

    static unsafe class UnsafeByteBufferUtil
    {
        internal static void GetBytes(UnsafeReadBuffer buf, byte* addr, int index, UnsafeReadBuffer dst, int dstIndex, int length)
        {
            Debug.Assert(dst != null);

            if (MathUtil.IsOutOfBounds(dstIndex, length, dst.Capacity))
            {
                ThrowHelper.ThrowIndexOutOfRangeException_DstIndex(dstIndex);
            }

            if (dst.HasMemoryAddress)
            {
                IntPtr ptr = dst.AddressOfPinnedMemory();
                if (ptr != IntPtr.Zero)
                {
                    PlatformDependent.CopyMemory(addr, (byte*)(ptr + dstIndex), length);
                }
                else
                {
                    fixed (byte* destination = &dst.GetPinnableMemoryAddress())
                    {
                        PlatformDependent.CopyMemory(addr, destination + dstIndex, length);
                    }
                }
            }
            else if (dst.HasArray)
            {
                PlatformDependent.CopyMemory(addr, dst.Array, dst.ArrayOffset + dstIndex, length);
            }
            else
            {
                dst.SetBytes(dstIndex, buf, index, length);
            }
        }

        internal static void GetBytes(UnsafeReadBuffer buf, byte* addr, int index, byte[] dst, int dstIndex, int length)
        {
            if (MathUtil.IsOutOfBounds(dstIndex, length, dst.Length))
            {
                ThrowHelper.ThrowIndexOutOfRangeException_DstIndex(dstIndex);
            }
            if (length != 0)
            {
                PlatformDependent.CopyMemory(addr, dst, dstIndex, length);
            }
        }

        internal static void SetBytes(UnsafeReadBuffer buf, byte* addr, int index, UnsafeReadBuffer src, int srcIndex, int length)
        {
            if (MathUtil.IsOutOfBounds(srcIndex, length, src.Capacity))
            {
                ThrowHelper.ThrowIndexOutOfRangeException_SrcIndex(srcIndex);
            }

            if (length != 0)
            {
                if (src.HasMemoryAddress)
                {
                    IntPtr ptr = src.AddressOfPinnedMemory();
                    if (ptr != IntPtr.Zero)
                    {
                        PlatformDependent.CopyMemory((byte*)(ptr + srcIndex), addr, length);
                    }
                    else
                    {
                        fixed (byte* source = &src.GetPinnableMemoryAddress())
                        {
                            PlatformDependent.CopyMemory(source + srcIndex, addr, length);
                        }
                    }
                }
                else if (src.HasArray)
                {
                    PlatformDependent.CopyMemory(src.Array, src.ArrayOffset + srcIndex, addr, length);
                }
                else
                {
                    src.GetBytes(srcIndex, buf, index, length);
                }
            }
        }
    }
}
