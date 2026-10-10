namespace MLInstallerSharp
{
    public static class Installer
    {
        private static readonly HttpClient Http = new HttpClient();

        public enum Architecture { x64, x86, linux, mac }

        /// <summary>
        /// Downloads the selected version to the target path.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if no download URL matches target architecture</exception>
        public static async Task DownloadVersionAsync(
            MLVersion version,
            string targetPath,
            Architecture arch,
            IProgress<double>? progress = null)
        {
            string? url = arch switch
            {
                Architecture.x64 => version.DownloadUrlWin,
                Architecture.x86 => version.DownloadUrlWinX86,
                Architecture.linux => version.DownloadUrlLinux,
                Architecture.mac => version.DownloadUrlMacOS,
                _ => null
            };

            if (url == null)
            {
                throw new ArgumentException($"No download URL for architecture {arch}", nameof(arch));
            }

            using HttpResponseMessage resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            long totalBytes = resp.Content.Headers.ContentLength ?? -1L;
            long downloadedBytes = 0L;

            await using FileStream fs = File.OpenWrite(targetPath);
            await using Stream stream = await resp.Content.ReadAsStreamAsync();

            byte[] buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, read));
                downloadedBytes += read;
                if (totalBytes > 0)
                {
                    double percent = downloadedBytes * 100.0 / totalBytes;
                    progress?.Report(percent);
                }
            }

            progress?.Report(100);
            return;
        }
    }
}
