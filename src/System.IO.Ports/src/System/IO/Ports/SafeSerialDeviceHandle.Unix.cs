// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Ports
{
    internal sealed class SafeSerialDeviceHandle : SafeHandle
    {
        private SafeSerialDeviceHandle() : base(new IntPtr(-1), ownsHandle: true)
        {
        }

        internal static SafeSerialDeviceHandle Open(string portName)
        {
            Debug.Assert(portName != null);
            SafeSerialDeviceHandle handle = Interop.Serial.SerialPortOpen(portName);

            if (handle.IsInvalid)
            {
                // exception type is matching Windows
                throw new UnauthorizedAccessException(
                    string.Format(SR.UnauthorizedAccess_IODenied_Port, portName),
                    Interop.GetIOException(Interop.Sys.GetLastErrorInfo()));
            }

            return handle;
        }

        protected override bool ReleaseHandle()
        {
            if (IsInvalid)
            {
                return false;
            }

            Interop.Serial.Shutdown(handle, SocketShutdown.Both);
            int result = Interop.Serial.SerialPortClose(handle);

            Debug.Assert(result == 0, string.Format(
                             "Close failed with result {0} and error {1}",
                             result, Interop.Sys.GetLastErrorInfo()));

            return result == 0;
        }

        public override bool IsInvalid
        {
            get
            {
                return (long)handle == -1;
            }
        }
    }
}
