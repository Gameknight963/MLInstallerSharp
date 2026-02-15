using System.Net.Http;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace MLInstallerSDK
{
    public class Installer
    {
        private static readonly HttpClient Http = new HttpClient();

        /// <summary>
        /// Downloads the selected version to the target path.
        /// </summary>
        public static async Task<bool> DownloadVersionAsync(MLVersion version, string targetPath, string? arch = "x64")
        {
            try
            {
                string? url = arch switch
                {
                    "x64" => version.DownloadUrlWin,
                    "x86" => version.DownloadUrlWinX86,
                    "linux" => version.DownloadUrlLinux,
                    "mac" => version.DownloadUrlMacOS,
                    _ => version.DownloadUrlWin
                };

                if (url == null) return false;

                using var resp = await Http.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                await using var fs = System.IO.File.OpenWrite(targetPath);
                await resp.Content.CopyToAsync(fs);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
