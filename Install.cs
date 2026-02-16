using System.Net.Http;
using System.Threading.Tasks;

namespace MLInstallerSDK
{
    public class Installer
    {
        private static readonly HttpClient Http = new HttpClient();

        public enum Architecture { x64, x86, linux, mac }

        /// <summary>
        /// Optional global logging for Installer actions
        /// </summary>
        public static Action<string>? Log { get; set; }

        private static void WriteLog(string message)
        {
            Log?.Invoke($"[Installer] {message}");
        }

        /// <summary>
        /// Downloads the selected version to the target path.
        /// </summary>
        public static async Task<bool> DownloadVersionAsync(
            MLVersion version,
            string targetPath,
            Architecture arch,
            IProgress<double>? progress = null) // percent 0–100
        {
            try
            {
                string? url = arch switch
                {
                    Architecture.x64 => version.DownloadUrlWin,
                    Architecture.x86 => version.DownloadUrlWinX86,
                    Architecture.linux => version.DownloadUrlLinux,
                    Architecture.mac => version.DownloadUrlMacOS,
                    _ => version.DownloadUrlWin
                };

                if (url == null)
                {
                    WriteLog($"No download URL for architecture {arch}");
                    return false;
                }

                WriteLog($"Starting download from {url} to {targetPath}...");
                using HttpResponseMessage resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();
                WriteLog("Download began");

                long totalBytes = resp.Content.Headers.ContentLength ?? -1L;
                long downloadedBytes = 0L;

                await using FileStream fs = System.IO.File.OpenWrite(targetPath);
                await using Stream stream = await resp.Content.ReadAsStreamAsync();

                byte[] buffer = new byte[81920];
                int read;
                WriteLog("Downloading content!");
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read));
                    downloadedBytes += read;
                    if (totalBytes > 0)
                    {
                        double percent = downloadedBytes * 100.0 / totalBytes;
                        progress?.Report(percent);
                        // WriteLog($"Downloaded {percent:0.00}% ({downloadedBytes}/{totalBytes} bytes)");
                    }
                }

                progress?.Report(100);
                WriteLog("Download complete!");
                return true;
            }
            catch (HttpRequestException ex)
            {
                WriteLog($"Download failed: {ex.Message}");
                return false;
            }
            catch (System.IO.IOException ex)
            {
                WriteLog($"File write failed: {ex.Message}");
                return false;
            }
            catch (System.Exception ex)
            {
                WriteLog($"Unexpected error: {ex.Message}");
                return false;
            }
        }
    }
}
