// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Runtime.InteropServices;

namespace SpoolerAccessPI.InteropHelpers
{
    public class NativeCodeException : SystemException
    {
        public int ErrorCode { get; private set; }
        public string NativeFunction { get; private set; }

        public NativeCodeException(string message, string nativeFunction)
            : base(message)
        {
            ErrorCode = Marshal.GetLastWin32Error();
            NativeFunction = nativeFunction;
        }
    }
}
