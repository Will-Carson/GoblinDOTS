// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class StreamConsumer<T>
        where T : StreamHandle
    {
        readonly Action<T, UnsafeReadBuffer> onAccept;
        readonly Action<T, Exception> onError;
        readonly Action<T> onCompleted;

        public StreamConsumer(
            Action<T, UnsafeReadBuffer> onAccept,
            Action<T, Exception> onError,
            Action<T> onCompleted)
        {
            Contract.Requires(onAccept != null);
            Contract.Requires(onError != null);

            this.onAccept = onAccept;
            this.onError = onError;
            this.onCompleted = onCompleted ?? OnCompleted;
        }

        public void Consume(T stream, UnsafeReadBuffer data, Exception error, bool completed)
        {
            try
            {
                if (error != null)
                {
                    onError(stream, error);
                }
                else
                {
                    onAccept(stream, data);
                }

                if (completed)
                {
                    onCompleted(stream);
                }
            }
            catch (Exception exception)
            {
                onError(stream, exception);
            }
        }

        static void OnCompleted(T stream) => stream.CloseHandle(OnClosed);

        static void OnClosed(StreamHandle streamHandle) => streamHandle.Dispose();
    }
}
