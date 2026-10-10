# MLInstallerSharp

A lightweight C# SDK for fetching and downloading MelonLoader stable releases and CI builds. Designed for integration into custom installers or tools, mainly because I'm making one and I needed a program like this

## Features

 - Fetch available versions from GitHub, stable releases or CI builds.

 - Download any version to a specified path or writable stream.

 - Report download progress and cancel downloads.

## Usage

Requires .NET 10.

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

Downloads return a ``Task`` and throw exceptions if they fail, instead of returning a success flag. A failed or cancelled download can leave a partial file.

### Download to a Stream
```csharp
using MLInstallerSharp;

MLVersion version = versions.First(v => v.DownloadUrlWin != null);

await using MemoryStream destination = new MemoryStream();
await Installer.DownloadVersionAsync(version, destination, Installer.Architecture.x64);

destination.Position = 0;
// Read or extract the downloaded archive from destination
```
The destination must be writable. Downloads start at its current position and leave the stream open, so you're responsible for disposing it.

## Logging

You can optionally attach a logger to MLManager.

Example

```csharp
using MLInstallerSharp;

MLManager.Log = message => Console.WriteLine(message);
```

Version discovery will emit messages:

```
[MLManager] Fetching CI builds...
```

## Progress Reporting

Both ``DownloadVersionAsync`` overloads support optional progress reporting via ``IProgress<double>``.

Progress is reported as a percentage, from 0-100. If the server doesn't provide the content length, only completion is reported.

### Example

```csharp
using MLInstallerSharp;

MLVersion version = versions.First(v => v.DownloadUrlWin != null);

Progress<double> progress = new Progress<double>(percent =>
{
    Console.WriteLine($"Download progress: {percent:0.0}%");
});

await Installer.DownloadVersionAsync(
    version,
    @"C:\Temp\MelonLoader.zip",
    Installer.Architecture.x64,
    progress
);

Console.WriteLine("Download complete!");
```

If you don't pass a progress object, progress reporting is simply disabled.

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

 - ``Task<List<MLVersion>> FetchAvailableVersionsAsync()``: Fetches stable releases and recent successful CI builds.

 - ``Action<string>? Log``: Optional logger for version discovery.

``Installer``

 - ``string GetDownloadUrl(MLVersion version, Architecture arch)``: Returns the selected platform's download URL without downloading. Throws if the version is null, the architecture is unsupported, or the URL is missing or isn't an absolute HTTP or HTTPS URL.

 - ``Task DownloadVersionAsync(MLVersion version, string targetPath, Architecture arch, IProgress<double>? progress = null, CancellationToken cancellationToken = default)``: Downloads the selected version to a file.

 - ``Task DownloadVersionAsync(MLVersion version, Stream destination, Architecture arch, IProgress<double>? progress = null, CancellationToken cancellationToken = default)``: Downloads the selected version to a writable stream.

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
