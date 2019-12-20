﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class ResolvedFrameworksProperty : IClassFixture<ResolvedFrameworksProperty.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;

        public ResolvedFrameworksProperty(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void ResolvedFrameworksPropertyIsEmptyForStandaloneApps()
        {
            var fixture = sharedTestState.StandaloneApp.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining($"Property RESOLVED_FRAMEWORKS = \r\n");
        }

        [Fact]
        public void ResolvedFrameworksPropertyIsComputedForPortableApps()
        {
            var fixture = sharedTestState.PortableApp.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var MNAversion = sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion;

            dotnet.Exec(appDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining($"Property RESOLVED_FRAMEWORKS = Framework:Microsoft.NETCore.App,Requested:{"2.1.0"},Resolved:{MNAversion}\r\n");
        }

        public class SharedTestState : IDisposable
        {
            public RepoDirectoriesProvider RepoDirectories { get; }
            public TestProjectFixture PortableApp { get; }
            public TestProjectFixture StandaloneApp { get; }
            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider(microsoftNETCoreAppVersion: "2.1.0");
                var rid = RepoDirectories.TargetRID;
                StandaloneApp = new TestProjectFixture("StandaloneApp", RepoDirectories)
                    .BuildProject(runtime: rid, restore: true);

                PortableApp = new TestProjectFixture("PortableApp21", RepoDirectories, framework: "netcoreapp2.1", assemblyName: "PortableApp")
                    .BuildProject(restore: true);
            }

            public void Dispose()
            {
                PortableApp.Dispose();
                StandaloneApp.Dispose();
            }
        }
    }
}
