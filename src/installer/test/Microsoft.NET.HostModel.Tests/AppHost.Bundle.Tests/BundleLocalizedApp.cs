// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.Bundle;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleLocalizedApp : IClassFixture<BundleLocalizedApp.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleLocalizedApp(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void RunTheApp(string path)
        {
            Command.Create(path)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("ನಮಸ್ಕಾರ! வணக்கம்! नमस्ते! Hello! ");
        }

        // BundleOptions.BundleNativeBinaries: Test when the payload data files are unbundled, and beside the single-file app.
        // BundleOptions.BundleAllContent: Test when the payload data files are bundled and extracted to temporary directory. 
        // Once the runtime can load assemblies from the bundle, BundleOptions.None can be used in place of BundleOptions.BundleNativeBinaries.
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Localized_App_Run_Succeeds(BundleOptions options)
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture, options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                TestFixture = new TestProjectFixture("LocalizedApp", RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(TestFixture));
            }

            public void Dispose()
            {
                //TestFixture.Dispose();
            }
        }
    }
}
