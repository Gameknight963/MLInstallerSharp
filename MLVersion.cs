using Semver;
using System;
using System.Collections.Generic;
using System.Text;

namespace MLInstallerSharp
{
    public class MLVersion
    {
        public string? DownloadUrlWin { get; init; }
        public string? DownloadUrlWinX86 { get; init; }
        public string? DownloadUrlLinux { get; init; }
        public string? DownloadUrlMacOS { get; init; }
        public required SemVersion Version { get; init; }
        public bool IsLocalPath { get; init; } 
    }
}
