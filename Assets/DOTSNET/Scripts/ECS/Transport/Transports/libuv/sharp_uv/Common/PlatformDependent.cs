// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace NetUV.Core.Common
{
    using System;
    using System.Runtime.CompilerServices;
    using NetUV.Core.Native;

    static class PlatformDependent
    {
        static readonly bool IsLinux = Platform.IsLinux;

        public static unsafe void CopyMemory(byte* src, byte* dst, int length)
        {
            if (length > 0)
            {
                if (IsLinux)
                {
                    Buffer.MemoryCopy(src, dst, length, length);
                }
                else
                {
                    Unsafe.CopyBlockUnaligned(dst, src, unchecked((uint)length));
                }
            }
        }

        public static unsafe void CopyMemory(byte* src, byte[] dst, int dstIndex, int length)
        {
            if (length > 0)
            {
                fixed (byte* destination = &dst[dstIndex])
                    if (IsLinux)
                    {
                        Buffer.MemoryCopy(src, destination, length, length);
                    }
                    else
                    {
                        Unsafe.CopyBlockUnaligned(destination, src, unchecked((uint)length));
                    }
            }
        }

        public static unsafe void CopyMemory(byte[] src, int srcIndex, byte* dst, int length)
        {
            if (length > 0)
            {
                fixed (byte* source = &src[srcIndex])
                    if (IsLinux)
                    {
                        Buffer.MemoryCopy(source, dst, length, length);
                    }
                    else
                    {
                        Unsafe.CopyBlockUnaligned(dst, source, unchecked((uint)length));
                    }
            }
        }
    }
}