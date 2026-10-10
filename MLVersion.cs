using Semver;
using System;
using System.Collections.Generic;
using System.Text;

namespace MLInstallerSharp
{
    /// <summary>A MelonLoader version and its platform download URLs.</summary>
    public class MLVersion
    {
        /// <summary>Windows x64 archive URL, if available.</summary>
        public string? DownloadUrlWin { get; init; }
        /// <summary>Windows x86 archive URL, if available.</summary>
        public string? DownloadUrlWinX86 { get; init; }
        /// <summary>Linux x64 archive URL, if available.</summary>
        public string? DownloadUrlLinux { get; init; }
        /// <summary>macOS x64 archive URL, if available.</summary>
        public string? DownloadUrlMacOS { get; init; }
        /// <summary>The MelonLoader version number.</summary>
        public required SemVersion Version { get; init; }
        /// <summary>Caller-provided metadata; downloads do not use this flag.</summary>
        public bool IsLocalPath { get; init; }
    }
}
