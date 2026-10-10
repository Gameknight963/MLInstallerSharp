# MLInstallerSharp

A lightweight C# SDK for fetching and downloading MelonLoader stable releases and CI builds. Designed for integration into custom installers or tools, mainly because I'm making one and I needed a program like this

## Features

 - Fetch available versions from GitHub, stable releases or CI builds.

 - Download any version to a specified path or writable stream.

 - Report download progress and cancel downloads.

## Usage

Targets .NET Standard 2.0, so it be used with .NET Framework 4.7.2 or later and modern .NET.

### Fetch Available Versions
```csharp
using MLInstallerSharp;

List<MLVersion> versions = await MLManager.FetchAvailableVersionsAsync();

foreach (MLVersion v in versions)
{
    Console.WriteLine($"{v.Version} - x64: {v.DownloadUrlWin}");
}
```

### Download a Version
```csharp
using MLInstallerSharp;

// Pick a version from the fetched list with a Windows x64 download
MLVersion version = versions.First(v => v.DownloadUrlWin != null);

// Download to a target path
await Installer.DownloadVersionAsync(version, @"C:\Temp\MelonLoader.zip", Installer.Architecture.x64);
// you probably don't want to download there, adjust the path as needed

Console.WriteLine("Download complete!");
```
You'll need to extract the zip after the download is complete. The target directory must already exist, and an existing file at the target path is overwritten.

Downloads return a ``Task`` and throw exceptions if they fail (which are annotated in XML). A failed or cancelled download can leave a partial file which you are responsible for cleaning up.

### Download to a Stream
```csharp
using MLInstallerSharp;

MLVersion version = versions.First(v => v.DownloadUrlWin != null);

using MemoryStream destination = new MemoryStream();
await Installer.DownloadVersionAsync(version, destination, Installer.Architecture.x64);

destination.Position = 0;
// Read or extract the downloaded archive from destination
```
The destination must be writable. Downloads start at its current position and leave the stream open, so you're responsible for disposing it.

### Fetch Progress

``FetchAvailableVersionsAsync`` accepts an optional ``Action<FetchStage>`` callback. Stages are ``FetchingCIBuilds``, ``FetchingStableReleases``, and ``Completed``.

```csharp
using MLInstallerSharp;

List<MLVersion> versions = await MLManager.FetchAvailableVersionsAsync(
    progress: stage => Console.WriteLine($"Fetch stage: {stage}")
);
```

### Download Progress

Both ``DownloadVersionAsync`` overloads accept an optional ``Action<DownloadStage, double?>`` callback with two arguments: stage and percentage.

Stages are ``Connecting``, ``Downloading``, and ``Completed``. The percentage is null while connecting or when the server doesn't provide the content length. Successful completion reports 100.

```csharp
using MLInstallerSharp;

MLVersion version = versions.First(v => v.DownloadUrlWin != null);

await Installer.DownloadVersionAsync(
    version,
    @"C:\Temp\MelonLoader.zip",
    Installer.Architecture.x64,
    progress: (stage, percent) =>
    {
        Console.WriteLine(percent.HasValue
            ? $"{stage}: {percent.Value:0.0}%"
            : $"{stage}...");
    }
);
```

Callbacks run inline and don't automatically dispatch to a UI thread. If you're updating a UI, dispatch the update yourself. Exceptions thrown by a callback propagate to the caller.

Progress reports normal activity. Failures still throw exceptions for the caller to handle and log; failed operations don't report ``Completed``.

## Cancellation

Both download overloads accept an optional ``CancellationToken``.

```csharp
using MLInstallerSharp;

MLVersion version = versions.First(v => v.DownloadUrlWin != null);
using CancellationTokenSource cancellation = new CancellationTokenSource();
cancellation.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    await Installer.DownloadVersionAsync(
        version,
        @"C:\Temp\MelonLoader.zip",
        Installer.Architecture.x64,
        cancellationToken: cancellation.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Download cancelled.");
}
```

## API

``MLManager``

 - ``Task<List<MLVersion>> FetchAvailableVersionsAsync(Action<FetchStage>? progress = null)``: Fetches stable releases and recent successful CI builds.

``Installer``

 - ``string GetDownloadUrl(MLVersion version, Architecture arch)``: Returns the selected platform's download URL without downloading. Throws if the version is null, the architecture is unsupported, or the URL is missing or isn't an absolute HTTP or HTTPS URL.

 - ``Task DownloadVersionAsync(MLVersion version, string targetPath, Architecture arch, Action<DownloadStage, double?>? progress = null, CancellationToken cancellationToken = default)``: Downloads the selected version to a file.

 - ``Task DownloadVersionAsync(MLVersion version, Stream destination, Architecture arch, Action<DownloadStage, double?>? progress = null, CancellationToken cancellationToken = default)``: Downloads the selected version to a writable stream.

``MLVersion``

 - ``SemVersion Version``: The MelonLoader version number.

 - ``string? DownloadUrlWin, DownloadUrlWinX86, DownloadUrlLinux, DownloadUrlMacOS``: Direct download URLs. A URL can be null if that platform isn't available.

 - ``bool IsLocalPath``: Caller-provided metadata. The downloader doesn't use this flag or read local archives.

## Architecture Enum

``Installer``

```csharp
public enum Architecture
{
    x64, //win64
    x86, //win32
    linux, //linux64
    mac //mac64
}
```
Architecture enum added by me. Not sure why the melonloader devs opted for a string. downloads. The names are not perfectly clear because it's designed to mirror the internal Melonloader apis used in the installer
### Notes

 - CI builds are fetched from the five most recent successful push runs of the alpha-development branch GitHub Actions workflow.

 - Stable releases are fetched from the official MelonLoader releases API. Only the first page is currently fetched.
