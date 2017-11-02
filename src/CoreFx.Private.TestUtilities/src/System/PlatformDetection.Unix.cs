// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System
{
    public static partial class PlatformDetection
    {
        public static bool IsWindowsIoTCore => false;
        public static bool IsWindows => false;
        public static bool IsWindows7 => false;
        public static bool IsWindows8x => false;
        public static bool IsWindows10Version1607OrGreater => false;
        public static bool IsWindows10Version1703OrGreater => false;
        public static bool IsWindows10InsiderPreviewBuild16215OrGreater => false;
        public static bool IsWindows10Version16251OrGreater => false;
        public static bool IsNotOneCoreUAP =>  true;
        public static bool IsNetfx462OrNewer() { return false; }
        public static bool IsNetfx470OrNewer() { return false; }
        public static bool IsNetfx471OrNewer() { return false; }
        public static bool IsInAppContainer => false;
        public static int WindowsVersion => -1;

        public static bool IsOpenSUSE => IsDistroAndVersion("opensuse");
        public static bool IsUbuntu => IsDistroAndVersion("ubuntu");
        public static bool IsDebian => IsDistroAndVersion("debian");
        public static bool IsDebian8 => IsDistroAndVersion("debian", new Version("8"));
        public static bool IsUbuntu1404 => IsDistroAndVersion("ubuntu", new Version("14.04"));
        public static bool IsUbuntu1604 => IsDistroAndVersion("ubuntu", new Version("16.04"));
        public static bool IsUbuntu1704 => IsDistroAndVersion("ubuntu", new Version("17.04"));
        public static bool IsUbuntu1710 => IsDistroAndVersion("ubuntu", new Version("17.10"));
        public static bool IsTizen => IsDistroAndVersion("tizen");
        public static bool IsFedora => IsDistroAndVersion("fedora");
        public static bool IsWindowsNanoServer => false;
        public static bool IsWindowsServerCore => false;
        public static bool IsWindowsAndElevated => false;
        public static bool IsWindowsRedStone2 => false;

        // RedHat family covers RedHat and CentOS
        public static bool IsRedHatFamily => IsRedHatFamilyAndVersion();
        public static bool IsNotRedHatFamily => !IsRedHatFamily;
        public static bool IsRedHatFamily6 => IsRedHatFamilyAndVersion("6");
        public static bool IsNotRedHatFamily6 => !IsRedHatFamily6;
        public static bool IsRedHatFamily7 => IsRedHatFamilyAndVersion("7");
        public static bool IsNotFedoraOrRedHatFamily => !IsFedora && !IsRedHatFamily;

        public static Version OSXKernelVersion { get; } = GetOSXKernelVersion();

        public static string GetDistroVersionString()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX Version=" + s_osxProductVersion.ToString();
            }

            DistroInfo v = GetDistroInfo();

            return "Distro=" + v.Id + " VersionId=" + v.VersionId;
        }

        private static readonly Version s_osxProductVersion = GetOSXProductVersion();

        public static bool IsMacOsHighSierraOrHigher { get; } =
            IsOSX && (s_osxProductVersion.Major > 10 || (s_osxProductVersion.Major == 10 && s_osxProductVersion.Minor >= 13));

        private static readonly Version s_icuVersion = GetICUVersion();
        public static Version ICUVersion => s_icuVersion;

        private static Version GetICUVersion()
        {
            int ver = GlobalizationNative_GetICUVersion();
            return new Version( ver & 0xFF,
                               (ver >> 8)  & 0xFF,
                               (ver >> 16) & 0xFF,
                                ver >> 24);
        }

        private static DistroInfo GetDistroInfo() => new DistroInfo()
        {
            Id = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystem,
            VersionId = new Version(Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystemVersion)
        };

        private static bool IsRedHatFamilyAndVersion(string versionId = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DistroInfo v = GetDistroInfo();

                // RedHat includes minor version. We need to account for that when comparing
                if ((v.Id == "rhel" || v.Id == "centos") && VersionEquivalentWith(versionId, v.VersionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool VersionEquivalentWith(Version expectedVersionId, Version actualVersionId)
        {
            if (expectedVersionId == null)
            {
                return true;
            }

            string[] expected = expectedVersionId.Split('.');
            string[] actual = actualVersionId.Split('.');

            if (expected.Length > actual.Length)
            {
                return false;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string RemoveQuotes(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                // Remove quotes.
                s = s.Substring(1, s.Length - 2);
            }

            return s;
        }

        private struct DistroInfo
        {
            public string Id { get; set; }
            public Version VersionId { get; set; }
        }

        /// <summary>
        /// Get whether the OS platform matches the given Linux distro and optional version.
        /// </summary>
        /// <param name="distroId">The distribution id.</param>
        /// <param name="versionId">The distro version.  If omitted, compares the distro only.</param>
        /// <returns>Whether the OS platform matches the given Linux distro and optional version.</returns>
        private static bool IsDistroAndVersion(string distroId, Version versionId = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DistroInfo v = GetDistroInfo();
                if (v.Id == distroId && (versionId == null || v.VersionId == versionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static Version GetOSXKernelVersion()
        {
            if (IsOSX)
            {
                byte[] bytes = new byte[256];
                IntPtr bytesLength = new IntPtr(bytes.Length);
                Assert.Equal(0, sysctlbyname("kern.osrelease", bytes, ref bytesLength, null, IntPtr.Zero));
                string versionString = Encoding.UTF8.GetString(bytes);
                return Version.Parse(versionString);
            }

            return new Version(0, 0, 0);
        }

        private static Version GetOSXProductVersion()
        {
            try
            {
                if (IsOSX)
                {
                    // <plist version="1.0">
                    // <dict>
                    //         <key>ProductBuildVersion</key>
                    //         <string>17A330h</string>
                    //         <key>ProductCopyright</key>
                    //         <string>1983-2017 Apple Inc.</string>
                    //         <key>ProductName</key>
                    //         <string>Mac OS X</string>
                    //         <key>ProductUserVisibleVersion</key>
                    //         <string>10.13</string>
                    //         <key>ProductVersion</key>
                    //         <string>10.13</string>
                    // </dict>
                    // </plist>

                    XElement dict = XDocument.Load("/System/Library/CoreServices/SystemVersion.plist").Root.Element("dict");
                    if (dict != null)
                    {
                        foreach (XElement key in dict.Elements("key"))
                        {
                            if ("ProductVersion".Equals(key.Value))
                            {
                                XElement stringElement = key.NextNode as XElement;
                                if (stringElement != null && stringElement.Name.LocalName.Equals("string"))
                                {
                                    string versionString = stringElement.Value;
                                    if (versionString != null)
                                    {
                                        return Version.Parse(versionString);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            // In case of exception or couldn't get the version 
            return new Version(0, 0, 0);
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int sysctlbyname(string ctlName, byte[] oldp, ref IntPtr oldpLen, byte[] newp, IntPtr newpLen);

        [DllImport("libc", SetLastError = true)]
        internal static extern unsafe uint geteuid();

        [DllImport("System.Globalization.Native", SetLastError = true)]
        private static extern int GlobalizationNative_GetICUVersion();

        public static bool IsSuperUser => geteuid() == 0;
    }
}
