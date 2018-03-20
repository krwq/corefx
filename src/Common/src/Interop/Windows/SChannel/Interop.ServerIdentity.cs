// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal static partial class SChannel
    {
        [DllImport(Libraries.SChannel, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int SslGetServerIdentity(byte* clientHello, int clientHelloSize, out byte* serverIdentity, out int serverIdentitySize, int flags);
    }
}
