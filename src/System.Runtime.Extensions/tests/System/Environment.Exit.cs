// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using Xunit;

namespace System.Tests
{
    public class Environment_Exit : RemoteExecutorTestBase
    {
        public static object[][] ExitCodeValues = new object[][]
        {
            new object[] { 0 },
            new object[] { 1 },
            new object[] { 42 },
            new object[] { -1 },
            new object[] { -45 },
            new object[] { 255 },
        };

        [Theory]
        [MemberData(nameof(ExitCodeValues))]
        public static void CheckExitCode(int expectedExitCode)
        {
            RemoteInvoke(s => int.Parse(s), expectedExitCode.ToString(), new RemoteInvokeOptions { ExpectedExitCode = expectedExitCode }).Dispose();
        }

        [Theory]
        [MemberData(nameof(ExitCodeValues))]
        public static void ExitCode_Roundtrips(int exitCode)
        {
            Environment.ExitCode = exitCode;
            Assert.Equal(exitCode, Environment.ExitCode);

            Environment.ExitCode = 0; // in case the test host has a void returning Main
        }

        [Theory]
        [InlineData(1)] // setting ExitCode and exiting Main
        [InlineData(2)] // setting ExitCode both from Main and from an Unloading event handler.
        [InlineData(3)] // using Exit(exitCode)
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20387 - ILC test pipeline does not accomodate tests in child processes built into custom assemblies.", TargetFrameworkMonikers.UapAot)]
        public static void ExitCode_VoidMainAppReturnsSetValue(int mode)
        {
            int expectedExitCode = 123;
            const string AppName = "VoidMainWithExitCodeApp.exe";
            var psi = new ProcessStartInfo();

            bool fwdOut = true;
            if (fwdOut)
            {
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
            }

            if (PlatformDetection.IsUap)
            {
                //psi.FileName = "corerun.exe";
                //psi.Arguments = $"{AppName} {expectedExitCode} {mode}";

                psi.FileName = AppName;
                psi.Arguments = $"{expectedExitCode} {mode}";

            }
            else if (PlatformDetection.IsFullFramework || PlatformDetection.IsNetNative)
            {
                psi.FileName = AppName;
                psi.Arguments = $"{expectedExitCode} {mode}";
            }
            else
            {
                psi.FileName = HostRunner;
                psi.Arguments = $"{AppName} {expectedExitCode} {mode}";
            }

            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                if (fwdOut)
                {
                    throw new Exception( 
                        $"Intentionally failing...\n" +
                        $"Expected exit code: {expectedExitCode}\n" +
                        $"Actual exit code: {p.ExitCode}\n" +
                        $"psi.FileName = {psi.FileName}\n" +
                        $"psi.Arguments = {psi.Arguments}\n" +
                        $"StdOut:\n{p.StandardOutput.ReadToEnd()}\n\n" +
                        $"StdErr:\n{p.StandardError.ReadToEnd()}\n" +
                        $"--------END-------\n");
                }
                Assert.Equal(expectedExitCode, p.ExitCode);
            }
        }

        [Fact]
        public void Whensdfsdfsd()
        {
            RemoteInvoke(() =>
            {
                Environment.Exit(997);
                //Assert.True(false);
                return SuccessExitCode;
            }).Dispose();
        }
    }
}
