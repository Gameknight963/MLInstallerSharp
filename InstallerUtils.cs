using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Semver;

namespace MLInstallerSharp
{
    /// <summary>Fetches available MelonLoader releases and CI builds.</summary>
    public static class MLManager
    {
        private static readonly HttpClient Http = new HttpClient();

        private const string StableReleasesApi = "https://api.github.com/repos/LavaGang/MelonLoader/releases";
        private const string CIWorkflowApi = "https://api.github.com/repos/LavaGang/MelonLoader/actions/workflows/5411546/runs?branch=alpha-development&event=push&status=success&per_page=5";

        /// <summary>Optional global logging for version discovery.</summary>
        public static Action<string>? Log { get; set; }

        private static void WriteLog(string message)
        {
            Log?.Invoke($"[MLManager] {message}");
        }

        static MLManager()
        {
            Http.DefaultRequestHeaders.Add("User-Agent", "MLInstallerSharp");
        }

        /// <summary>
        /// Fetches all available versions (stable + CI) and returns them in a list.
        /// </summary>
        public static async Task<List<MLVersion>> FetchAvailableVersionsAsync()
        {
            List<MLVersion> versions = new List<MLVersion>();
            WriteLog("Starting fetch of available MelonLoader versions...");

            // --- CI builds ---
            WriteLog("Fetching CI builds...");
            using HttpResponseMessage ciResp = await Http.GetAsync(CIWorkflowApi);
            ciResp.EnsureSuccessStatusCode();
            WriteLog("CI builds response received.");

            string ciJson = await ciResp.Content.ReadAsStringAsync();
            JsonArray runs = JsonNode.Parse(ciJson)?["workflow_runs"]?.AsArray()
                ?? throw new InvalidOperationException("Invalid CI API response.");

            int ciCount = 0;
            foreach (JsonNode? run in runs)
            {
                if (run == null) continue;
                string? runName = run["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(runName)) continue;

                int verEnd = runName.IndexOf(' ');
                if (verEnd <= 0) continue;

                if (!SemVersion.TryParse(runName[..verEnd], SemVersionStyles.Any, out SemVersion? runVersion))
                    continue;

                versions.Add(new MLVersion
                {   
                    Version = runVersion,
                    DownloadUrlWin = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Windows.x64.CI.Release.zip",
                    DownloadUrlWinX86 = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Windows.x86.CI.Release.zip",
                    DownloadUrlLinux = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Linux.x64.CI.Release.zip",
                    DownloadUrlMacOS = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.macOS.x64.CI.Release.zip"
                });
                ciCount++;
            }
            WriteLog($"CI builds processed: {ciCount}");

            // --- Stable releases ---
            WriteLog("Fetching stable releases...");
            using HttpResponseMessage stableResp = await Http.GetAsync(StableReleasesApi);
            stableResp.EnsureSuccessStatusCode();
            WriteLog("Stable releases response received.");

            string stableJson = await stableResp.Content.ReadAsStringAsync();
            JsonArray releases = JsonNode.Parse(stableJson)?.AsArray()
                ?? throw new InvalidOperationException("Invalid releases API response.");

            int stableCount = 0;
            foreach (JsonNode? release in releases)
            {
                if (!SemVersion.TryParse(release?["tag_name"]?.ToString(),
                    SemVersionStyles.Any, out SemVersion? relVersion))
                    continue;

                JsonArray? assets = release?["assets"]?.AsArray();
                if (assets == null) continue;

                versions.Add(new MLVersion
                {
                    Version = relVersion,
                    DownloadUrlWin = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x64.zip")?["browser_download_url"]?.ToString(),
                    DownloadUrlWinX86 = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.x86.zip")?["browser_download_url"]?.ToString(),
                    DownloadUrlLinux = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.Linux.x64.zip")?["browser_download_url"]?.ToString(),
                    DownloadUrlMacOS = assets.FirstOrDefault(x => x?["name"]?.ToString() == "MelonLoader.macOS.x64.zip")?["browser_download_url"]?.ToString()
                });
                stableCount++;
            }
            WriteLog($"Stable releases processed: {stableCount}");

            WriteLog($"Total versions fetched: {versions.Count}");
            return versions;
        }
    }
}
