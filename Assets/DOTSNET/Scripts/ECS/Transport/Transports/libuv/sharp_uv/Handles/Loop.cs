// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using NetUV.Core.Common;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    public sealed class Loop : IDisposable
    {
        internal static readonly ConcurrentPool<WriteRequest> WriteRequestPool = new ConcurrentPool<WriteRequest>(() => new WriteRequest(uv_req_type.UV_WRITE, WriteRequestPool));

        readonly LoopContext handle;

        public Loop()
        {
            this.handle = new LoopContext();
        }

        public bool IsAlive => this.handle.IsAlive;

        public long Now => this.handle.Now;

        public long NowInHighResolution => this.handle.NowInHighResolution;

        public int ActiveHandleCount() => this.handle.ActiveHandleCount();

        public void UpdateTime() => this.handle.UpdateTime();

        internal int GetBackendTimeout() => this.handle.GetBackendTimeout();

        public int RunDefault() => this.handle.Run(uv_run_mode.UV_RUN_DEFAULT);

        public int RunOnce() => this.handle.Run(uv_run_mode.UV_RUN_ONCE);

        public int RunNoWait() => this.handle.Run(uv_run_mode.UV_RUN_NOWAIT);

        public void Stop() => this.handle.Stop();

        public Tcp CreateTcp()
        {
            this.handle.Validate();
            return new Tcp(this.handle);
        }

        public static Version NativeVersion => NativeMethods.GetVersion();

        public void Dispose() => this.handle.Dispose();
    }
}
