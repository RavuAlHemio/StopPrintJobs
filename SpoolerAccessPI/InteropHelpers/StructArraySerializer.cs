// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpoolerAccessPI.InteropHelpers
{
    /// <summary>
    /// Serializes an array of structs into contiguous memory and provides a pointer to the beginning of the first struct.
    /// </summary>
    internal class StructArraySerializer<T> : IDisposable
    {
        /// <summary>
        /// Contains the structs that will be serialized when <see cref="Serialize()"/> is called and deserialized
        /// when <see cref="Deserialize()"/> is called.
        /// </summary>
        public List<T> TheStructs { get; private set; }

        /// <summary>
        /// Once Serialize() is called, contains a pointer to the beginning of the first struct.
        /// </summary>
        public IntPtr PointerToFirstStruct { get; private set; }

        /// <summary>
        /// Once Serialize() is called, contains the size of one serialized struct.
        /// </summary>
        public int? StructSize { get; private set; }

        /// <summary>
        /// Once Serialize() is called, contains the number of structs that have been serialized.
        /// </summary>
        public int? SerializedCount { get; private set; }

        private bool IsDisposed { get; set; }

        /// <summary>
        /// Creates a new serializer for the given array of structs.
        /// </summary>
        /// <param name="theStructs">The structs for which to create the serializer.</param>
        public StructArraySerializer(IEnumerable<T> theStructs)
        {
            TheStructs = new List<T>(theStructs);
            PointerToFirstStruct = IntPtr.Zero;
            StructSize = null;
            SerializedCount = null;

            IsDisposed = false;
        }

        /// <summary>
        /// Creates a new serializer for an initially empty array of structs.
        /// </summary>
        public StructArraySerializer() : this(new T[] {})
        {
        }

        /// <summary>
        /// Serializes the structs, making PointerToFirstStruct point to the beginning of the first struct,
        /// setting StructSize to the size of one serialized struct, and setting SerializedCount to the number
        /// of structs that have been serialized.
        /// </summary>
        public void Serialize()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            StructSize = Marshal.SizeOf(typeof(T));

            // deallocate the previous pointers
            DeallocateSerializedStructs();

            // allocate the chunk
            PointerToFirstStruct = Marshal.AllocHGlobal(TheStructs.Count * StructSize.Value);

            // marshal the structs in sequence
            for (int i = 0; i < TheStructs.Count; ++i)
            {
                var thisStructPtr = IntPtr.Add(PointerToFirstStruct, i * StructSize.Value);
                Marshal.StructureToPtr(TheStructs[i], thisStructPtr, false);
            }

            // store SerializedCount
            SerializedCount = TheStructs.Count;
        }

        /// <summary>
        /// Deserializes a different struct array, loading the structs into TheStructs. Other public-facing
        /// properties remain untouched.
        /// </summary>
        public void Deserialize(int count, IntPtr pointerToContiguousStructs)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var oneSize = Marshal.SizeOf(typeof(T));

            TheStructs.Clear();
            for (int i = 0; i < count; ++i)
            {
                var thisStructPtr = IntPtr.Add(pointerToContiguousStructs, i * oneSize);
                TheStructs.Add((T)Marshal.PtrToStructure(thisStructPtr, typeof(T)));
            }
        }

        /// <summary>
        /// Deserializes the last serialized struct array, loading the structs back into TheStructs.
        /// </summary>
        public void Deserialize()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            // clear the old list
            TheStructs.Clear();
            for (int i = 0; i < SerializedCount.Value; ++i)
            {
                var thisStructPtr = IntPtr.Add(PointerToFirstStruct, i * StructSize.Value);
                TheStructs.Add((T)Marshal.PtrToStructure(thisStructPtr, typeof(T)));
            }
        }

        protected void DeallocateSerializedStructs()
        {
            if (PointerToFirstStruct != IntPtr.Zero)
            {
                for (int i = 0; i < SerializedCount; ++i)
                {
                    // destroy contained struct fields
                    var thisStructPtr = IntPtr.Add(PointerToFirstStruct, i * StructSize.Value);
                    Marshal.DestroyStructure(thisStructPtr, typeof(T));
                }
                Marshal.FreeHGlobal(PointerToFirstStruct);
                PointerToFirstStruct = IntPtr.Zero;
            }
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

                // clean up all the allocated stuff
                DeallocateSerializedStructs();
            }
            IsDisposed = true;
        }

        ~StructArraySerializer()
        {
            Dispose(false);
        }
    }
}
