using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics; // For Debug.WriteLine
using System.Net.Http.Headers;

namespace yt_dlp_gui.App {
    public static class YtdlpUpdater {
        private static readonly string YtdlpExecutableName = "yt-dlp.exe";
        private static readonly string GitHubApiUrl = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string?> CheckAndUpdateYtdlpAsync() {
            try {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("yt-dlp-gui-updater");

                HttpResponseMessage response = await httpClient.GetAsync(GitHubApiUrl);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject releaseInfo = JObject.Parse(jsonResponse);

                string? downloadUrl = null;
                if (releaseInfo["assets"] is JArray assets) {
                    foreach (JToken asset in assets) {
                        if (asset["name"]?.ToString() == YtdlpExecutableName) {
                            downloadUrl = asset["browser_download_url"]?.ToString();
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl)) {
                    Debug.WriteLine("yt-dlp.exe not found in release assets.");
                    return null;
                }

                string targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "yt-dlp-gui", "yt-dlp");
                Directory.CreateDirectory(targetDirectory);
                string targetPath = Path.Combine(targetDirectory, YtdlpExecutableName);

                byte[] fileBytes = await httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(targetPath, fileBytes);

                Debug.WriteLine($"yt-dlp.exe downloaded to: {targetPath}");
                return targetPath;

            } catch (HttpRequestException ex) {
                Debug.WriteLine($"HTTP request error: {ex.Message}");
                return null;
            } catch (Newtonsoft.Json.JsonException ex) {
                Debug.WriteLine($"JSON parsing error: {ex.Message}");
                return null;
            } catch (IOException ex) {
                Debug.WriteLine($"File I/O error: {ex.Message}");
                return null;
            } catch (Exception ex) {
                Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
                return null;
            }
        }
    }
}
