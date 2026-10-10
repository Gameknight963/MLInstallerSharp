namespace MLInstallerSharp
{
    /// <summary>Downloads MelonLoader archives to files or streams.</summary>
    public static class Installer
    {
        private static readonly HttpClient Http = new HttpClient();

        /// <summary>Supported download platforms.</summary>
        public enum Architecture
        {
            /// <summary>Windows x64.</summary>
            x64,
            /// <summary>Windows x86.</summary>
            x86,
            /// <summary>Linux x64.</summary>
            linux,
            /// <summary>macOS x64.</summary>
            mac
        }

        /// <summary>Returns the HTTP or HTTPS download URL for the selected platform.</summary>
        /// <exception cref="ArgumentNullException">The version is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The architecture is unsupported.</exception>
        /// <exception cref="ArgumentException">The selected URL is missing or invalid.</exception>
        public static string GetDownloadUrl(MLVersion version, Architecture arch)
        {
            ArgumentNullException.ThrowIfNull(version);
            string? url = arch switch
            {
                Architecture.x64 => version.DownloadUrlWin,
                Architecture.x86 => version.DownloadUrlWinX86,
                Architecture.linux => version.DownloadUrlLinux,
                Architecture.mac => version.DownloadUrlMacOS,
                _ => throw new ArgumentOutOfRangeException(nameof(arch), arch, "Unsupported architecture.")
            };
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException($"No download URL for architecture {arch}", nameof(version));
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Download URL must be an absolute HTTP or HTTPS URL.", nameof(version));
            return url;
        }

        /// <summary>
        /// Downloads the selected version to a writable stream, leaving it open.
        /// </summary>
        /// <exception cref="ArgumentNullException">The version or destination is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The architecture is unsupported.</exception>
        /// <exception cref="ArgumentException">The destination is not writable, or the selected URL is missing or invalid.</exception>
        /// <exception cref="HttpRequestException">The HTTP request fails or returns an unsuccessful status code.</exception>
        /// <exception cref="OperationCanceledException">The download is cancelled or the HTTP request times out.</exception>
        /// <exception cref="IOException">Reading the response or writing to the destination fails.</exception>
        /// <exception cref="ObjectDisposedException">A stream is disposed while the download is using it.</exception>
        public static async Task DownloadVersionAsync(
            MLVersion version,
            Stream destination,
            Architecture arch,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(destination);
            if (!destination.CanWrite)
                throw new ArgumentException("Destination stream must be writable.", nameof(destination));

            string url = GetDownloadUrl(version, arch);
            cancellationToken.ThrowIfCancellationRequested();

            using HttpResponseMessage resp = await Http.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resp.EnsureSuccessStatusCode();

            long totalBytes = resp.Content.Headers.ContentLength ?? -1L;
            long downloadedBytes = 0L;

            await using Stream source = await resp.Content.ReadAsStreamAsync(cancellationToken);

            byte[] buffer = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloadedBytes += read;

                if (totalBytes > 0)
                    progress?.Report(downloadedBytes * 100.0 / totalBytes);
            }

            progress?.Report(100);
        }

        /// <summary>
        /// Downloads the selected version to the target path.
        /// </summary>
        /// <exception cref="ArgumentNullException">The version or target path is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The architecture is unsupported.</exception>
        /// <exception cref="ArgumentException">The target path is empty or invalid, or the selected URL is missing or invalid.</exception>
        /// <exception cref="NotSupportedException">The target path uses an unsupported format.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the target file is denied, or the path refers to a directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The target directory cannot be found.</exception>
        /// <exception cref="PathTooLongException">The target path exceeds the system's length limit.</exception>
        /// <exception cref="IOException">Opening or writing the target file, or reading the response, fails.</exception>
        /// <exception cref="HttpRequestException">The HTTP request fails or returns an unsuccessful status code.</exception>
        /// <exception cref="OperationCanceledException">The download is cancelled or the HTTP request times out.</exception>
        public static async Task DownloadVersionAsync(
            MLVersion version,
            string targetPath,
            Architecture arch,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _ = GetDownloadUrl(version, arch);
            cancellationToken.ThrowIfCancellationRequested();
            await using FileStream fs = new(
                targetPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await DownloadVersionAsync(version, fs, arch, progress, cancellationToken);
        }
    }
}
