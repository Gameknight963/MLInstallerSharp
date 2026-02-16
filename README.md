# MelonLoader.SDK

A lightweight C# SDK for fetching and downloading MelonLoader stable releases and CI builds. Designed for integration into custom installers or tools, mainly because I'm making one and I needed a program like this

## Features

 - Fetch available versions from GitHub, stable releases or CI builds.

 - Download any version to a specified path.

## Usage
### Fetch Available Versions
```
using MLInstallerSDK;

List<MLVersion> versions = await MLManager.FetchAvailableVersionsAsync();

foreach (MLVersion v in versions)
{
    Console.WriteLine($"{v.Version} - x64: {v.DownloadUrlWin}");
}
```

### Download a Version
```
using MLInstallerSDK;

// Pick a version from the fetched list
MLVersion version = versions.First();

// Download to a target path
bool success = await Installer.DownloadVersionAsync(version, @"C:\Temp\MelonLoader.zip", Architecture.x64);
// you probably don't want to download there, adjust the path as needed

if (success)
    Console.WriteLine("Download complete!");
else
    Console.WriteLine("Download failed.");
```
You'll need to extract the zip after the download is complete.

## Logging

You can optionally attach a logger to both Installer and MLManager.

Example

```
using MLInstallerSDK;

Installer.Log = message => Console.WriteLine(message);
MLManager.Log = message => Console.WriteLine(message);

```

Now all internal SDK actions will emit messages:

```
[Installer] Starting download...
[MLManager] Fetching CI builds...
```


## Progress Reporting

``DownloadVersionAsync`` supports optional progress reporting via ``IProgress<double>``.

Progress is reported as a percentage, from 0-100.

### Example

```
using MLInstallerSDK;

MLVersion version = versions.First();

Progress<double> progress = new Progress<double>(percent =>
{
    Console.WriteLine($"Download progress: {percent:0.0}%");
});

bool success = await Installer.DownloadVersionAsync(
    version,
    @"C:\Temp\MelonLoader.zip",
    Installer.Architecture.x64,
    progress
);

if (success)
    Console.WriteLine("Download complete!");
else
    Console.WriteLine("Download failed.");

```

If you don’t pass a progress object, progress reporting is simply disabled.

## API
``MLManager``

 - ``Task<List<MLVersion>> FetchAvailableVersionsAsync()``: Fetches all stable + CI versions.

``Installer``

 - ``Task<bool> DownloadVersionAsync(MLVersion version, string targetPath, Architecture arch = Architecture.WindowsX6``4): Downloads the selected version to the target path

``MLVersion``

 - ``SemVersion Version``: The MelonLoader version number

 - ``string? DownloadUrlWin, DownloadUrlWinX86, DownloadUrlLinux, DownloadUrlMacOS``: Direct download URLs

 - ``bool IsLocalPath``: Indicates if this version is a local zip

##Architecture Enum
``Installer``

```
public enum Architecture
{
    x64,
    x86,
    linux,
    mac
}
```
Architecture  enum added by me. Not sure why the melonloader devs opted for a string.

### Notes

 - CI builds are fetched from the alpha-development branch GitHub Actions workflow.

 - Stable releases are fetched from the official MelonLoader releases API.