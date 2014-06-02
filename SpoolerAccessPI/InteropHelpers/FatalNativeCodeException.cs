// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Runtime.InteropServices;

namespace SpoolerAccessPI.InteropHelpers
{
    public class FatalNativeCodeException : NativeCodeException
    {
        public FatalNativeCodeException(string message, string nativeFunction)
            : base(message, nativeFunction)
        {
        }
    }
}
