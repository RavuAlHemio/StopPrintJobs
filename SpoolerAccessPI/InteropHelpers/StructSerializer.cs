// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Runtime.InteropServices;

namespace SpoolerAccessPI.InteropHelpers
{
    /// <summary>
    /// Serializes a struct into memory and provides a pointer to it.
    /// </summary>
    internal class StructSerializer<T> : IDisposable where T : new()
    {
        /// <summary>
        /// Contains the struct that will be serialized when <see cref="Serialize()"/> is called and deserialized
        /// when <see cref="Deserialize()"/> is called.
        /// </summary>
        public T TheStruct { get; set; }

        /// <summary>
        /// Once Serialize() is called, contains a pointer to a serialized version of the struct.
        /// </summary>
        public IntPtr StructPointer { get; private set; }

        /// <summary>
        /// Once Serialize() is called, contains the size of the serialized struct.
        /// </summary>
        public int? StructSize { get; private set; }

        private bool IsDisposed;

        /// <summary>
        /// Creates a new serializer for the given struct.
        /// </summary>
        /// <param name="theStruct">The struct for which to create the serializer.</param>
        public StructSerializer(T theStruct)
        {
            TheStruct = TheStruct;
            StructPointer = IntPtr.Zero;
            StructSize = null;
            IsDisposed = false;
        }

        /// <summary>
        /// Creates a new serializer for a struct filled with default values.
        /// </summary>
        public StructSerializer() : this(new T())
        {
        }

        /// <summary>
        /// Serializes the struct, making StructPointer point to the area in memory in which the struct is stored and
        /// setting StructSize to the size of the serialized struct.
        /// </summary>
        public void Serialize()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            bool freePrevious = true;

            if (StructPointer == IntPtr.Zero)
            {
                StructSize = Marshal.SizeOf(typeof(T));
                StructPointer = Marshal.AllocHGlobal(StructSize.Value);
                freePrevious = false;
            }

            Marshal.StructureToPtr(TheStruct, StructPointer, freePrevious);
        }

        /// <summary>
        /// Deserializes the struct, loading the contents of ptr into TheStruct.
        /// </summary>
        public void Deserialize(IntPtr ptr)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            Marshal.PtrToStructure(ptr, TheStruct);
        }

        /// <summary>
        /// Deserializes the last serialized struct, loading the contents of StructPointer into TheStruct.
        /// </summary>
        public void Deserialize()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            Deserialize(StructPointer);
        }

        /// <summary>
        /// Releases all resource used by the <see cref="SpoolerAccessPI.StructSerializer`1"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="SpoolerAccessPI.StructSerializer`1"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="SpoolerAccessPI.StructSerializer`1"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="SpoolerAccessPI.StructSerializer`1"/> so the garbage collector can reclaim the memory that the
        /// <see cref="SpoolerAccessPI.StructSerializer`1"/> was occupying.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                // if (disposing) { /* clean up managed resources */ }

                // clean up our allocated memory
                if (StructPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(StructPointer);
                }
            }
            IsDisposed = true;
        }

        ~StructSerializer()
        {
            Dispose(false);
        }
    }
}
