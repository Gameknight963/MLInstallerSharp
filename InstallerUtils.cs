using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Semver;

namespace MLInstallerSDK
{
    public static class MLManager
    {
        private static readonly HttpClient Http = new HttpClient();

        private const string StableReleasesApi = "https://api.github.com/repos/LavaGang/MelonLoader/releases";
        private const string CIWorkflowApi = "https://api.github.com/repos/LavaGang/MelonLoader/actions/workflows/5411546/runs?branch=alpha-development&event=push&status=success&per_page=5";

        static MLManager()
        {
            Http.DefaultRequestHeaders.Add("User-Agent", "MelonLoader.SDK");
        }

        /// <summary>
        /// Fetches all available versions (stable + CI) and returns them in a list.
        /// </summary>
        public static async Task<List<MLVersion>> FetchAvailableVersionsAsync()
        {
            var versions = new List<MLVersion>();

            // --- CI builds ---
            var ciResp = await Http.GetAsync(CIWorkflowApi);
            ciResp.EnsureSuccessStatusCode();

            var ciJson = await ciResp.Content.ReadAsStringAsync();
            var runs = JsonNode.Parse(ciJson)?["workflow_runs"]?.AsArray()
                ?? throw new InvalidOperationException("Invalid CI API response.");

            foreach (var run in runs)
            {
                var runName = run?["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(runName)) continue;

                var verEnd = runName.IndexOf(' ');
                if (verEnd <= 0) continue;

                if (!SemVersion.TryParse(runName[..verEnd], SemVersionStyles.Any, out var runVersion))
                    continue;
                #pragma warning disable CS8602 // Dereference of a possibly null reference.
                versions.Add(new MLVersion
                {
                    Version = runVersion,
                    DownloadUrlWin = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Windows.x64.CI.Release.zip",
                    DownloadUrlWinX86 = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Windows.x86.CI.Release.zip",
                    DownloadUrlLinux = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Linux.x64.CI.Release.zip",
                    DownloadUrlMacOS = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.macOS.x64.CI.Release.zip"
                });
                #pragma warning restore CS8602
            }

            // --- Stable releases ---
            var stableResp = await Http.GetAsync(StableReleasesApi);
            stableResp.EnsureSuccessStatusCode();

            var stableJson = await stableResp.Content.ReadAsStringAsync();
            var releases = JsonNode.Parse(stableJson)?.AsArray()
                ?? throw new InvalidOperationException("Invalid releases API response.");

            foreach (var release in releases)
            {
                if (!SemVersion.TryParse(release?["tag_name"]?.ToString(),
                    SemVersionStyles.Any, out var relVersion))
                    continue;

                var assets = release?["assets"]?.AsArray();
                if (assets == null) continue;

                versions.Add(new MLVersion
                {
                    Version = relVersion,
                    DownloadUrlWin = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x64.zip")?["browser_download_url"]?.ToString(),
                    DownloadUrlWinX86 = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x86.zip")?["browser_download_url"]?.ToString(),
                    DownloadUrlLinux = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.Linux.x64.zip")?["browser_download_url"]?.ToString(),
                    DownloadUrlMacOS = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.macOS.x64.zip")?["browser_download_url"]?.ToString()
                });
            }

            return versions;
        }

    }
}
