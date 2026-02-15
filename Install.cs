using System.Net.Http;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace MLInstallerSDK
{
    public class Installer
    {
        private static readonly HttpClient Http = new HttpClient();

        public enum Architecture { x64, x86, linux, mac }

        /// <summary>
        /// Downloads the selected version to the target path.
        /// </summary>
        public static async Task<bool> DownloadVersionAsync(MLVersion version, string targetPath, Architecture arch = Architecture.x64)
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
