// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Runtime.InteropServices;

namespace SpoolerAccessPI.InteropHelpers
{
    internal class HGlobalBytes : IDisposable
    {
        /// <summary>
        /// The pointer pointing to the allocated memory.
        /// </summary>
        public IntPtr Pointer { get; private set; }

        /// <summary>
        /// The number of bytes that have been allocated, as an integer converted into a pointer.
        /// </summary>
        public IntPtr PtrCount { get; private set; }

        /// <summary>
        /// The number of bytes that have been allocated, as a 32-bit integer.
        /// </summary>
        public int Count
        {
            get
            {
                return PtrCount.ToInt32();
            }
        }

        /// <summary>
        /// The number of bytes that have been allocated, as a 64-bit integer.
        /// </summary>
        public long LongCount
        {
            get
            {
                return PtrCount.ToInt64();
            }
        }

        private bool IsDisposed { get; set; }

        /// <summary>
        /// Allocate a specific number of bytes on the global heap, accessible via Pointer.
        /// </summary>
        /// <param name="count">Number of bytes to allocate.</param>
        public HGlobalBytes(int count) : this(new IntPtr(count))
        {
        }

        /// <summary>
        /// Allocate a specific number of bytes on the gloal heap, accessible via Pointer.
        /// </summary>
        /// <param name="count"> Number of bytes to allocate, as an integer converted into a pointer.</param>
        public HGlobalBytes(IntPtr count)
        {
            Pointer = Marshal.AllocHGlobal(count);
            IsDisposed = false;
        }

        /// <summary>
        /// Change the size of the allocation.
        /// </summary>
        /// <param name="count">The new number of bytes, as an integer converted into a pointer.</param>
        /// <remarks>Note that the value of Pointer might change after a call to Realloc.</remarks>
        public void Realloc(IntPtr count)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            Pointer = Marshal.ReAllocHGlobal(Pointer, count);
        }

        /// <summary>
        /// Change the size of the allocation.
        /// </summary>
        /// <param name="count">The new number of bytes, as a 32-bit integer.</param>
        /// <remarks>Note that the value of Pointer might change after a call to Realloc.</remarks>
        public void Realloc(int count)
        {
            Realloc(new IntPtr(count));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                // if (disposing) { /* dispose managed resources */ }

                Marshal.FreeHGlobal(Pointer);
                Pointer = IntPtr.Zero;
            }
            IsDisposed = true;
        }

        ~HGlobalBytes()
        {
            Dispose(false);
        }
    }
}

