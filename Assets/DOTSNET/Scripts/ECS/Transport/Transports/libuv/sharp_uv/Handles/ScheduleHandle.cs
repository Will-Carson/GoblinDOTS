// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using NetUV.Core.Logging;
    using NetUV.Core.Native;

    public abstract class ScheduleHandle : IDisposable
    {
        readonly HandleContext handle;
        Action<ScheduleHandle> closeCallback;

        internal ScheduleHandle(
            LoopContext loop,
            uv_handle_type handleType,
            object[] args = null)
        {
            Contract.Requires(loop != null);

            HandleContext initialHandle = NativeMethods.Initialize(loop.Handle, handleType, this, args);
            Debug.Assert(initialHandle != null);

            this.handle = initialHandle;
            this.HandleType = handleType;
        }

        public bool IsActive => this.handle.IsActive;

        public bool IsClosing => this.handle.IsClosing;

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.handle.IsValid;
        }

        public object UserToken { get; set; }

        public IntPtr InternalHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.handle.Handle;
        }

        internal uv_handle_type HandleType { get; }

        internal void OnHandleClosed()
        {
            try
            {
                this.handle.SetHandleAsInvalid();
                this.closeCallback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} close handle callback error.", exception);
            }
            finally
            {
                this.closeCallback = null;
                this.UserToken = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Validate() => this.handle.Validate();

        protected internal void CloseHandle(Action<ScheduleHandle> handler = null)
        {
            try
            {
                this.ScheduleClose(handler);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} Failed to close handle.", exception);
                throw;
            }
        }

        protected virtual void ScheduleClose(Action<ScheduleHandle> handler = null)
        {
            if (!this.IsValid)
            {
                return;
            }

            this.closeCallback = handler;
            this.Close();
            this.handle.Dispose();
        }

        protected abstract void Close();

        public void Dispose()
        {
            try
            {
                this.CloseHandle();
            }
            catch (Exception exception)
            {
                Log.Warn($"{this.handle} Failed to close and releasing resources.", exception);
            }
        }
    }
}
