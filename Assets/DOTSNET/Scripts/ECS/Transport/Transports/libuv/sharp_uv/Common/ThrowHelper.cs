﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable NotResolvedInText
// ReSharper disable UseNameofExpression
// ReSharper disable UseStringInterpolation
namespace NetUV.Core.Buffers
{
    using System;
    using System.Runtime.CompilerServices;

    static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIndexOutOfRangeException_Index(int index, int length, int capacity)
        {
            throw GetIndexOutOfRangeException();

            IndexOutOfRangeException GetIndexOutOfRangeException()
            {
                return new IndexOutOfRangeException(string.Format("index: {0}, length: {1} (expected: range(0, {2}))", index, length, capacity));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIndexOutOfRangeException_SrcIndex(int srcIndex)
        {
            throw GetIndexOutOfRangeException();

            IndexOutOfRangeException GetIndexOutOfRangeException()
            {
                return new IndexOutOfRangeException(string.Format("srcIndex: {0}", srcIndex));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIndexOutOfRangeException_DstIndex(int dstIndex)
        {
            throw GetIndexOutOfRangeException();

            IndexOutOfRangeException GetIndexOutOfRangeException()
            {
                return new IndexOutOfRangeException(string.Format("dstIndex: {0}", dstIndex));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIndexOutOfRangeException_ReaderIndex(int minimumReadableBytes, int readerIndex, int writerIndex, UnsafeReadBuffer buf)
        {
            throw GetIndexOutOfRangeException();

            IndexOutOfRangeException GetIndexOutOfRangeException()
            {
                return new IndexOutOfRangeException(string.Format("readerIndex({0}) + length({1}) exceeds writerIndex({2}): {3}", readerIndex, minimumReadableBytes, writerIndex, buf));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIndexOutOfRangeException_WriterIndex(int index, int readerIndex, int capacity)
        {
            throw GetIndexOutOfRangeException();

            IndexOutOfRangeException GetIndexOutOfRangeException()
            {
                return new IndexOutOfRangeException(string.Format("WriterIndex: {0} (expected: 0 <= readerIndex({1}) <= writerIndex <= capacity ({2})", index, readerIndex, capacity));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalReferenceCountException(int count)
        {
            throw GetIllegalReferenceCountException();

            IllegalReferenceCountException GetIllegalReferenceCountException()
            {
                return new IllegalReferenceCountException(count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalReferenceCountException(int count, int increment)
        {
            throw GetIllegalReferenceCountException();

            IllegalReferenceCountException GetIllegalReferenceCountException()
            {
                return new IllegalReferenceCountException(count, increment);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_MinimumReadableBytes(int minimumReadableBytes)
        {
            throw GetArgumentOutOfRangeException();

            ArgumentOutOfRangeException GetArgumentOutOfRangeException()
            {
                return new ArgumentOutOfRangeException("minimumReadableBytes", string.Format("minimumReadableBytes: {0} (expected: >= 0)", minimumReadableBytes));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_InitialCapacity()
        {
            throw GetArgumentOutOfRangeException();

            ArgumentOutOfRangeException GetArgumentOutOfRangeException()
            {
                return new ArgumentOutOfRangeException("initialCapacity", "initialCapacity must be greater than zero");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_InitialCapacity(int initialCapacity, int maxCapacity)
        {
            throw GetArgumentOutOfRangeException();

            ArgumentOutOfRangeException GetArgumentOutOfRangeException()
            {
                return new ArgumentOutOfRangeException("initialCapacity", string.Format("initialCapacity ({0}) must be greater than maxCapacity ({1})", initialCapacity, maxCapacity));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_WriteRequest()
        {
            throw GetInvalidOperationException();

            InvalidOperationException GetInvalidOperationException()
            {
                return new ObjectDisposedException("WriteRequest status is invalid.");
            }
        }
    }
}
