namespace MLInstallerSharp
{
    public static class Installer
    {
        private static readonly HttpClient Http = new HttpClient();

        public enum Architecture { x64, x86, linux, mac }

        /// <summary>
        /// Downloads the selected version to the target path.
        /// </summary>
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

            string? url = arch switch
            {
                Architecture.x64 => version.DownloadUrlWin,
                Architecture.x86 => version.DownloadUrlWinX86,
                Architecture.linux => version.DownloadUrlLinux,
                Architecture.mac => version.DownloadUrlMacOS,
                _ => null
            };
            if (url == null)
                throw new ArgumentException($"No download URL for architecture {arch}", nameof(arch));

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
        public static async Task DownloadVersionAsync(
            MLVersion version,
            string targetPath,
            Architecture arch,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
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
