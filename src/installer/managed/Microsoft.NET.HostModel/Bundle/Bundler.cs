// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.NET.HostModel.AppHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection.PortableExecutable;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// Bundler: Functionality to embed the managed app and its dependencies
    /// into the host native binary.
    /// </summary>
    public class Bundler
    {
        readonly string HostName;
        readonly string OutputDir;
        readonly string DepsJson;
        readonly string RuntimeConfigJson;
        readonly string RuntimeConfigDevJson;
        public readonly Manifest BundleManifest;

        readonly Trace trace;
        readonly TargetRuntime targetRuntime;
        readonly BundleOptions bundleOptions;
        readonly float TFMVersion;

        public static string Version => (Manifest.MajorVersion + "." + Manifest.MinorVersion);

        public Bundler(string hostName, string outputDir,
                       bool embedPDBs = false,
                       bool diagnosticOutput = false,
                       OperatingSystem targetOS = OperatingSystem.Unknown,
                       float targetFrameworkVersion = (float)3.0,
                       BundleOptions options = BundleOptions.None)
        {
            HostName = hostName;
            OutputDir = Path.GetFullPath(string.IsNullOrEmpty(outputDir) ? Environment.CurrentDirectory : outputDir);

            string baseName = Path.GetFileNameWithoutExtension(HostName);
            DepsJson = baseName + ".deps.json";
            RuntimeConfigJson = baseName + ".runtimeconfig.json";
            RuntimeConfigDevJson = baseName + ".runtimeconfig.dev.json";

            trace = new Trace(diagnosticOutput);
            TFMVersion = targetFrameworkVersion;
            BundleManifest = new Manifest();
            targetRuntime = new TargetRuntime(targetOS);
            bundleOptions = options;
        }

        /// <summary>
        /// Embed 'file' into 'bundle'
        /// </summary>
        /// <returns>Returns the offset of the start 'file' within 'bundle'</returns>

        long AddToBundle(Stream bundle, Stream file, FileType type)
        {
            if (type == FileType.Assembly)
            {
                // Align embedded assemblies such that they can be efficiently mapped from the bundle.
                const int assemblyAlignment = 16;
                long misalignment = (bundle.Position % assemblyAlignment);

                if (misalignment != 0)
                {
                    long padding = assemblyAlignment - misalignment;
                    bundle.Position += padding;
                }
            }

            file.Position = 0;
            long startOffset = bundle.Position;
            file.CopyTo(bundle);

            return startOffset;
        }

        bool ShouldEmbed(string fileRelativePath)
        {
            return fileRelativePath.Equals(HostName) ||
                   fileRelativePath.Equals(RuntimeConfigDevJson);
        }

        bool ShouldEmbed(FileType type)
        {
            switch (type)
            {
                case FileType.Assembly:
                case FileType.DepsJson:
                case FileType.RuntimeConfigJson:
                    return true;

                case FileType.NativeBinary:
                    return bundleOptions.HasFlag(BundleOptions.BundleNativeBinaries);

                case FileType.Symbols:
                    return bundleOptions.HasFlag(BundleOptions.BundleSymbolFiles);

                default:
                    return bundleOptions.HasFlag(BundleOptions.BundleOtherFiles);
            }
        }


        bool IsAssembly(string path, out bool isPE)
        {
            isPE = false;

            using (FileStream file = File.OpenRead(path))
            {
                try
                {
                    PEReader peReader = new PEReader(file);
                    CorHeader corHeader = peReader.PEHeaders.CorHeader;

                    isPE = true; // If peReader.PEHeaders doesn't throw, it is a valid PEImage
                    return corHeader != null;
                }
                catch (BadImageFormatException)
                {
                }
            }

            return false;
        }

        FileType InferType(string fileRelativePath)
        {
            if (fileRelativePath.Equals(DepsJson))
            {
                return FileType.DepsJson;
            }

            if (fileRelativePath.Equals(RuntimeConfigJson))
            {
                return FileType.RuntimeConfigJson;
            }

            if (Path.GetExtension(fileRelativePath).ToLowerInvariant().Equals(".pdb"))
            {
                return FileType.Symbols;
            }

            bool isPE;
            if (IsAssembly(fileRelativePath, out isPE))
            {
                return FileType.Assembly;
            }

            bool isNativeBinary = targetRuntime.IsWindows ? isPE : targetRuntime.IsNativeBinary(fileRelativePath);

            if (isNativeBinary)
            {
                return FileType.NativeBinary;
            }

            return FileType.Other;
        }

        /// <summary>
        /// Generate a bundle, given the specification of embedded files
        /// </summary>
        /// <param name="fileSpecs">
        /// An enumeration FileSpecs for the files to be embedded.
        /// </param>
        /// <returns>
        /// The full path the the generated bundle file
        /// </returns>
        /// <exceptions>
        /// ArgumentException if input is invalid
        /// IOExceptions and ArgumentExceptions from callees flow to the caller.
        /// </exceptions>
        public string GenerateBundle(IReadOnlyList<FileSpec> fileSpecs)
        {
            trace.Log($"Bundler version {Version}");

            if (fileSpecs.Any(x => !x.IsValid()))
            {
                throw new ArgumentException("Invalid input specification: Found entry with empty source-path or bundle-relative-path.");
            }

            string hostSource;
            try
            {
                hostSource = fileSpecs.Where(x => x.BundleRelativePath.Equals(HostName)).Single().SourcePath;
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("Invalid input specification: Must specify the host binary");
            }

            if (fileSpecs.GroupBy(file => file.BundleRelativePath).Where(g => g.Count() > 1).Any())
            {
                throw new ArgumentException("Invalid input specification: Found multiple entries with the same BundleRelativePath");
            }

            string bundlePath = Path.Combine(OutputDir, HostName);
            if (File.Exists(bundlePath))
            {
                trace.Log($"Ovewriting existing File {bundlePath}");
            }

            BinaryUtils.CopyFile(hostSource, bundlePath);

            long headerOffset = 0;
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(bundlePath)))
            {
                Stream bundle = writer.BaseStream;
                bundle.Position = bundle.Length;

                List<Tuple<FileSpec, FileType>> ailgnedFiles = new List<Tuple<FileSpec, FileType>>();

                foreach (var fileSpec in fileSpecs)
                {
                    if (!ShouldEmbed(fileSpec.BundleRelativePath))
                    {
                        trace.Log($"Skip: {fileSpec.BundleRelativePath}");
                        continue;
                    }

                    FileType type = InferType(fileSpec.BundleRelativePath);

                    if (!ShouldEmbed(type))
                    {
                        trace.Log($"Skip {type}: {fileSpec.BundleRelativePath}");
                        continue;
                    }

                    using (FileStream file = File.OpenRead(fileSpec.SourcePath))
                    {
                        long startOffset = AddToBundle(bundle, file, type);
                        FileEntry entry = BundleManifest.AddEntry(type, fileSpec.BundleRelativePath, startOffset, file.Length);
                        trace.Log($"Embed: {entry}");
                    }
                }

                // Write the bundle manifest
                headerOffset = BundleManifest.Write(writer);
                trace.Log($"Header Offset={headerOffset}");
                trace.Log($"Meta-data Size={writer.BaseStream.Position - headerOffset}");
                trace.Log($"Bundle: Path={bundlePath}, Size={bundle.Length}");
            }

            HostWriter.SetAsBundle(bundlePath, headerOffset);

            return bundlePath;
        }

        string RelativePath(string dirFullPath, string fileFullPath)
        {
            // This function is used in lieu of Path.GetRelativePath because
            //   * Path.GetRelativePath() doesn't exist in netstandard2.0
            //   * This implementation is pretty much only intended for testing.
            //     SDK integration invokes GenerateBundle(fileSpecs) directly.
            // 
            // In later revisions, we should target netstandard2.1, and replace 
            // this function with Path.GetRelativePath().

            return fileFullPath.Substring(dirFullPath.TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Generate a bundle containind the (embeddable) files in sourceDir
        /// </summary>
        public string GenerateBundle(string sourceDir)
        {
            // Convert sourceDir to absolute path
            sourceDir = Path.GetFullPath(sourceDir);

            // Get all files in the source directory and all sub-directories.
            string[] sources = Directory.GetFiles(sourceDir, searchPattern: "*", searchOption: SearchOption.AllDirectories);

            // Sort the file names to keep the bundle construction deterministic.
            Array.Sort(sources, StringComparer.Ordinal);

            List<FileSpec> fileSpecs = new List<FileSpec>(sources.Length);
            foreach (var file in sources)
            {
                fileSpecs.Add(new FileSpec(file, RelativePath(sourceDir, file)));
            }

            return GenerateBundle(fileSpecs);
        }
    }
}

