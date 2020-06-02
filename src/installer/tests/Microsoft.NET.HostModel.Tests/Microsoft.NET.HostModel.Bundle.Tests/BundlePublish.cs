// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using BundleTests.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.Tests
{
    public class BundlePublish : IClassFixture<BundleLegacy.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundlePublish(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void AppPublishedFromSdkRuns()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.GetPublishedSingleFilePath(fixture);

            Command.Create(singleFile)
                   .CaptureStdErr()
                   .CaptureStdOut()
                   .Execute()
                   .Should()
                   .Pass()
                   .And
                   .HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void TestExpectedFiles()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var publishDir = BundleHelper.GetPublishDir(fixture);

            string[] expectedFiles;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedFiles = new string[]
                {
                    BundleHelper.GetSingleFileName(fixture),
                    BundleHelper.GetPdbName(fixture),
                    "coreclr.dll",
                    "clrjit.dll",
                    "clrcompression.dll",
                    "mscordaccore.dll"
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                expectedFiles = new string[]
                {
                    BundleHelper.GetSingleFileName(fixture),
                    BundleHelper.GetPdbName(fixture),
                    "libcoreclr.so",
                    "libclrjit.so",
                    "libSystem.Globalization.Native.so",
                    "libSystem.IO.Compression.Native.so",
                    "libSystem.IO.Ports.Native.so",
                    "libSystem.Native.so",
                    "libSystem.Net.Security.Native.so",
                    "libSystem.Security.Cryptography.Native.OpenSsl.so"
                };
            }
            else
            {
                expectedFiles = new string[]
                {
                    BundleHelper.GetSingleFileName(fixture),
                    BundleHelper.GetPdbName(fixture),
                    "libcoreclr.dylib",
                    "libclrjit.dylib",
                    "libSystem.Globalization.Native.dylib",
                    "libSystem.IO.Compression.Native.dylib",
                    "libSystem.IO.Ports.Native.dylib",
                    "libSystem.Native.dylib",
                    "libSystem.Net.Security.Native.dylib",
                    "libSystem.Security.Cryptography.Native.Apple.dylib",
                    "libSystem.Security.Cryptography.Native.OpenSsl.dylib"
                };
            }

            publishDir.Should().OnlyHaveFiles(expectedFiles);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("StaticHostApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid,
                                    singleFile: true,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
