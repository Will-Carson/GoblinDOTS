// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace NetUV.Core.Common
{
    using System;

    static class PlatformDependent
    {
        public static unsafe void CopyMemory(byte* src, byte* dst, int length)
        {
            if (length > 0)
            {
                Buffer.MemoryCopy(src, dst, length, length);
            }
        }

        public static unsafe void CopyMemory(byte* src, byte[] dst, int dstIndex, int length)
        {
            if (length > 0)
            {
                fixed (byte* destination = &dst[dstIndex])
                    Buffer.MemoryCopy(src, destination, length, length);
            }
        }

        public static unsafe void CopyMemory(byte[] src, int srcIndex, byte* dst, int length)
        {
            if (length > 0)
            {
                fixed (byte* source = &src[srcIndex])
                    Buffer.MemoryCopy(source, dst, length, length);
            }
        }

        public static unsafe void* AsPointer<T>(ref T value)
            where T : struct
        {
            // Unsafe.* is not available in Unity with hybrid renderer 0.7
            // because they made it internal.
#if UNITY_5_6_OR_NEWER
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref value);
#else
            return Unsafe.AsPointer(ref value);
#endif
        }
    }
}