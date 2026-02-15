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
bool success = await Installer.DownloadVersionAsync(version, @"C:\Temp\MelonLoader.zip", Architecture.WindowsX64);

if (success)
    Console.WriteLine("Download complete!");
else
    Console.WriteLine("Download failed.");
```
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

# Notes

 - CI builds are fetched from the alpha-development branch GitHub Actions workflow.

 - Stable releases are fetched from the official MelonLoader releases API.

‍

Yes I used a pragma. What are you gonna do about it? It won't even be a problem unless Github's API undergoes major changes (at which point you probably have bigger fish to fry.)