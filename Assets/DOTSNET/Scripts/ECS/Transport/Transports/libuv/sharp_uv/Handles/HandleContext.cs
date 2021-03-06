// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetUV.Core.Logging;

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using NetUV.Core.Native;

    sealed unsafe class HandleContext : NativeHandle
    {
        static readonly uv_close_cb CloseCallback = OnCloseHandle;
        readonly uv_handle_type handleType;

        internal HandleContext(
            uv_handle_type handleType,
            Func<IntPtr, IntPtr, object[], int> initializer,
            IntPtr loopHandle,
            ScheduleHandle target,
            params object[] args)
        {
            Debug.Assert(loopHandle != IntPtr.Zero);
            Debug.Assert(initializer != null);
            Debug.Assert(target != null);

            int size = NativeMethods.GetSize(handleType);
            IntPtr handle = Marshal.AllocCoTaskMem(size);

            try
            {
                int result = initializer(loopHandle, handle, args);
                NativeMethods.ThrowIfError(result);
            }
            catch (Exception)
            {
                Marshal.FreeCoTaskMem(handle);
                throw;
            }

            GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
            ((uv_handle_t*)handle)->data = GCHandle.ToIntPtr(gcHandle);

            this.Handle = handle;
            this.handleType = handleType;

            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("{0} {1} allocated.", handleType, handle);
            }
        }

        internal bool IsActive => this.IsValid
            && NativeMethods.IsHandleActive(this.Handle);

        internal bool IsClosing => this.IsValid
            && NativeMethods.IsHandleClosing(this.Handle);

        internal void AddReference()
        {
            this.Validate();
            NativeMethods.AddReference(this.Handle);
        }

        internal void ReleaseReference()
        {
            this.Validate();
            NativeMethods.ReleaseReference(this.Handle);
        }

        internal bool HasReference()
        {
            this.Validate();
            return NativeMethods.HadReference(this.Handle);
        }

        protected override void CloseHandle()
        {
            IntPtr handle = this.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.CloseHandle(handle, CloseCallback);
            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("{0} {1} closed, releasing resources pending.", this.handleType, handle);
            }
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            IntPtr internalHandle = ((uv_handle_t*)handle)->data;
            if (internalHandle != IntPtr.Zero)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(internalHandle);
                if (gcHandle.IsAllocated)
                {
                    return (T)gcHandle.Target;
                }
            }

            return default(T);
        }

        static void OnCloseHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            ScheduleHandle scheduleHandle = null;

            // Get gc handle first
            IntPtr pHandle = ((uv_handle_t*)handle)->data;
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    scheduleHandle = nativeHandle.Target as ScheduleHandle;
                    nativeHandle.Free();

                    ((uv_handle_t*)handle)->data = IntPtr.Zero;
                }
            }

            // Release memory
            Marshal.FreeCoTaskMem(handle);
            scheduleHandle?.OnHandleClosed();
        }
    }
}
