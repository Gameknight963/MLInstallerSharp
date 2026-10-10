using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Semver;

namespace MLInstallerSharp
{
    /// <summary>Stages of MelonLoader version discovery.</summary>
    public enum FetchStage
    {
        /// <summary>Fetching and processing recent CI builds.</summary>
        FetchingCIBuilds,
        /// <summary>Fetching and processing stable releases.</summary>
        FetchingStableReleases,
        /// <summary>Version discovery completed successfully.</summary>
        Completed
    }

    /// <summary>Fetches available MelonLoader releases and CI builds.</summary>
    public static class MLManager
    {
        private static readonly HttpClient Http = new HttpClient();

        private const string StableReleasesApi = "https://api.github.com/repos/LavaGang/MelonLoader/releases";
        private const string CIWorkflowApi = "https://api.github.com/repos/LavaGang/MelonLoader/actions/workflows/5411546/runs?branch=alpha-development&event=push&status=success&per_page=5";

        static MLManager()
        {
            Http.DefaultRequestHeaders.Add("User-Agent", "MLInstallerSharp");
        }

        /// <summary>
        /// Fetches all stable releases and recent CI builds and returns them in a list.
        /// </summary>
        /// <remarks>The optional progress callback runs inline and propagates any exceptions it throws.</remarks>
        /// <exception cref="HttpRequestException">A GitHub API request fails or returns an unsuccessful status code.</exception>
        /// <exception cref="OperationCanceledException">An HTTP request times out.</exception>
        /// <exception cref="System.Text.Json.JsonException">A GitHub API response contains invalid JSON.</exception>
        /// <exception cref="InvalidOperationException">A GitHub API response is missing the expected data or has an unexpected JSON structure.</exception>
        /// <exception cref="IOException">Reading a response fails.</exception>
        public static async Task<List<MLVersion>> FetchAvailableVersionsAsync(Action<FetchStage>? progress = null)
        {
            List<MLVersion> versions = new List<MLVersion>();

            // --- CI builds ---
            progress?.Invoke(FetchStage.FetchingCIBuilds);
            using HttpResponseMessage ciResp = await Http.GetAsync(CIWorkflowApi);
            ciResp.EnsureSuccessStatusCode();

            string ciJson = await ciResp.Content.ReadAsStringAsync();
            JsonArray runs = JsonNode.Parse(ciJson)?["workflow_runs"]?.AsArray()
                ?? throw new InvalidOperationException("Invalid CI API response.");

            foreach (JsonNode? run in runs)
            {
                string? runName = run!["name"]?.ToString();
                if (runName == null || string.IsNullOrWhiteSpace(runName)) continue;

                int verEnd = runName.IndexOf(' ');
                if (verEnd <= 0) continue;

                if (!SemVersion.TryParse(runName.Substring(0, verEnd), SemVersionStyles.Any, out SemVersion? runVersion))
                    continue;

                versions.Add(new MLVersion
                {   
                    Version = runVersion,
                    DownloadUrlWin = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Windows.x64.CI.Release.zip",
                    DownloadUrlWinX86 = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Windows.x86.CI.Release.zip",
                    DownloadUrlLinux = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.Linux.x64.CI.Release.zip",
                    DownloadUrlMacOS = $"https://nightly.link/LavaGang/MelonLoader/actions/runs/{run["id"]}/MelonLoader.macOS.x64.CI.Release.zip"
                });
            }

            // --- Stable releases ---
            progress?.Invoke(FetchStage.FetchingStableReleases);
            const int pageSize = 100;
            for (int page = 1; ; page++)
            {
                using HttpResponseMessage stableResp = await Http.GetAsync($"{StableReleasesApi}?per_page={pageSize}&page={page}");
                stableResp.EnsureSuccessStatusCode();

                string stableJson = await stableResp.Content.ReadAsStringAsync();
                JsonArray releases = JsonNode.Parse(stableJson)?.AsArray()
                    ?? throw new InvalidOperationException("Invalid releases API response.");

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
                }

                if (releases.Count < pageSize)
                    break;
            }

            progress?.Invoke(FetchStage.Completed);
            return versions;
        }
    }
}
