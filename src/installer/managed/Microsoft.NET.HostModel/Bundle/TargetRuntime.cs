// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.NET.HostModel.AppHost;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// TargetRuntime: The target runtime for which the single-file bundle is built.
    /// 
    /// Currently the TargetRuntime only tracks the target operating system.
    /// If necessary, the target architecture may be tracked in future.
    /// </summary>

    public class TargetRuntime
    {
        public OperatingSystem OS;

        public TargetRuntime(OperatingSystem os)
        {
            OS = os;
        }

        public bool IsNativeBinary(string filePath)
        {
            switch (OS)
            {
                case OperatingSystem.Linux:
                    return ElfUtils.IsElfImage(filePath);

                case OperatingSystem.Osx:
                    return MachOUtils.IsMachOImage(filePath);

                case OperatingSystem.Windows:
                    return PEUtils.IsPEImage(filePath);

                default:
                    return false;
            }
        }

        public bool IsLinux => OS == OperatingSystem.Linux;
        public bool IsMac => OS == OperatingSystem.Osx;
        public bool IsWindows => OS == OperatingSystem.Windows;
    }
}

