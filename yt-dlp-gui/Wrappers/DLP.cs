using Libs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using yt_dlp_gui.App; // Required for YtdlpUpdater
using yt_dlp_gui.Models; // Required for Config

namespace yt_dlp_gui.Wrappers {

    public class DLP {
        public enum DLPType {
            yd_dlp, youtube_dl
        }
        static public DLPType Type { get; set; } = DLPType.yd_dlp;
        static public string Path_DLP { get; set; } = string.Empty;
        private static bool isYtdlpPathEnsured = false; // Added this line
        static public string Path_Aria2 { get; set; } = string.Empty;
        static public string Path_FFMPEG { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new List<string>();
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
        public string Url { get; set; } = string.Empty;
        public bool IsLive { get; set; } = false;
        public HashSet<DLPError> StdErr { get; set; } = new();
        Process process = new();
        public enum DLPError { Sign, Unsupported }

        public DLP(string url = "") {
            Url = url;
            Options["--no-playlist"] = "";
            //Options["--no-part"] = "";
            Options["--force-overwrites"] = "";
            Options["--ignore-config"] = "";
            Options["--ffmpeg-location"] = Path_FFMPEG.QP();
            if (Type == DLPType.yd_dlp) {
                Options["--progress-template"] = "\""
                    + "[yt-dlp]," //0
                    + "%(progress._percent_str)s," //1
                    + "%(progress._eta_str)s," //2
                    + "%(progress.downloaded_bytes)s," //3
                    + "%(progress.total_bytes)s," //4
                    + "%(progress.speed)s," //5
                    + "%(progress.eta)s" //6
                    + "\"";
                Options["--windows-filenames"] = "";
            }
        }
        public DLP LoadConfig(string path) {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) {
                Options.Remove("--ignore-config");
                Options["--config-location"] = path.QP();
            }
            return this;
        }
        public DLP Output(string targetpath) {
            Options["--output"] = targetpath.QP();
            return this;
        }
        public DLP MTime(ModifiedType type = ModifiedType.Modified) {
            if (type == ModifiedType.Created) {
                Options["--no-mtime"] = "";
            }
            return this;
        }
        public DLP Temp(string path) {
            Options["--cache-dir"] = path.QP();
            Options["[temp]"] = path.QP("temp");
            return this;
        }
        public DLP Proxy(string proxy_url, bool enable = true) {
            if (enable) {
                Options["--proxy"] = proxy_url.QS();
            }
            return this;
        }
        /*
        public DLP ExtractAudio(string format = "") {
            Options["--extract-audio"] = "";
            if (!string.IsNullOrWhiteSpace(format)) {
                Options["--audio-format"] = format.Trim('.').ToLower();
            }
            return this;
        }
        */
        public DLP DownloadSections(string regex) {
            if (!string.IsNullOrWhiteSpace(regex)) {
                Options["--download-sections"] = regex.QS();
            }
            return this;
        }
        public DLP SplitChapters(Chapters? chapters, string path = "") {
            if (chapters == null) return this; // Added null check
            switch (chapters.type) {
                case ChaptersType.Split:
                    Options["--split-chapters"] = "";
                    Options["[chapter]"] = path.suffix(" - %(section_number)03d. %(section_title)s").QP("chapter");
                    break;
                case ChaptersType.Segment:
                    if (!string.IsNullOrWhiteSpace(chapters.title)) {
                        Options["--download-sections"] = chapters.title.QS();
                    }
                    break;
            }
            return this;
        }
        public DLP EmbedChapters(bool enable) {
            if (enable) {
                Options["--embed-chapters"] = "";
            }
            return this;
        }
        public DLP Thumbnail(bool enable, string path, bool embed) {
            if (enable) { 
                if (embed) {
                    Options["--embed-thumbnail"] = "";
                } else {
                    Options["--write-thumbnail"] = "";
                    Options["--convert-thumbnails"] = "jpg";
                }
                var target = Path.ChangeExtension(path, ".%(ext)s").QP("thumbnail");
                Options["[thumbnail]"] = target;
                Files.Add(Path.ChangeExtension(path, ".jpg")); //predict
            }
            return this;
        }
        public DLP Subtitle(string? lang, string targetpath, bool embed) { // lang is now string?
            if (string.IsNullOrWhiteSpace(lang)) return this;
            Options["--sub-langs"] = lang;
            if (embed) {
                Options["--embed-subs"] = "";
            } else {
                Options["--write-subs"] = "";
                Options["--convert-subs"] = "srt";
                var target = Path.ChangeExtension(targetpath, ".%(ext)s").QP("subtitle");
                Options["[subtitle]"] = target;
                Files.Add(Path.ChangeExtension(targetpath, $".{lang}.srt"));
            }
            return this;
        }
        
        public DLP GetInfo() {
            Options["--dump-json"] = "";
            return this;
        }
        public DLP UseAria2(bool enable = true) {
            if (enable) {
                if (File.Exists(Path_Aria2)) {
                    Options["--external-downloader"] = Path_Aria2.QP();
                    //Options["--external-downloader"] = "aria2c";
                    //Options["--downloader-args"] = "aria2c:\"-x 16 -k 10M --user-agent=''\"";
                    if (Options.ContainsKey("--proxy")) {
                        Options["--external-downloader-args"] = "'--all-proxy=" + Options["--proxy"] + "'";
                    }
                }
            }
            return this;
        }
        public DLP LimitRate(string value) {
            if (!string.IsNullOrWhiteSpace(value)) {
                Options["--limit-rate"] = value;
            }
            return this;
        }
        public DLP Cookie(CookieType type, bool enable = true) {
            if (enable) {
                switch (type) {
                    case CookieType.Chrome:
                        Options["--cookies-from-browser"] = $"chrome";
                        break;
                    case CookieType.Edge:
                        Options["--cookies-from-browser"] = $"edge";
                        break;
                    case CookieType.Firefox:
                        Options["--cookies-from-browser"] = $"firefox";
                        break;
                    case CookieType.Opera:
                        Options["--cookies-from-browser"] = $"opera";
                        break;
                    case CookieType.Chromium:
                        Options["--cookies-from-browser"] = $"chromium";
                        break;
                    case CookieType.Chrome_Beta:
                        var AppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        var CookiePath = Path.Combine(AppData, "Google", "Chrome Beta");
                        Options["--cookies-from-browser"] = $"chrome:{CookiePath}";
                        break;
                }
            }
            return this;
        }
        private string Args {
            get {
                var args = Options.Select(x => {
                    var key = x.Key;
                    switch (key) {
                        case "[temp]":
                            //key = "--paths";
                            key = "--output";
                            break;
                        case "[chapter]":
                        case "[thumbnail]":
                        case "[subtitle]":
                            key = "--output";
                            break;
                    }
                    if (string.IsNullOrWhiteSpace(x.Value)) {
                        return key;
                    } else {
                        return $"{key} {x.Value}";
                    }
                }).ToList();
                args.Add(Url.QS());
                return string.Join(" ", args);
            }
        }
        public DLP DownloadFormat(string format_id, string targetpath, string originext) {
            Debug.WriteLine($"id:{format_id} path:{targetpath}", "DownloadFormat");
            Options["--format"] = format_id;
            if (targetpath.getExt() != originext) {
                Options["--remux-video"] = targetpath.getExt();
            }
            Options["--output"] = Path.ChangeExtension(targetpath, ".%(ext)s").QP();
            Files.Add(targetpath);
            return this;
        }
        public DLP DownloadVideo(string format_id, string? source_ext, string targetpath) { // source_ext is now string?
            Debug.WriteLine($"id:{format_id} source:{source_ext} path:{targetpath}", "DownloadVideo");
            Options["--format"] = format_id;
            // If source_ext is null, it won't match targetpath.getExt() unless targetpath.getExt() is also null (unlikely for getExt)
            // This logic seems okay even with source_ext being null.
            if (source_ext == targetpath.getExt()) {
                Options["--output"] = targetpath.QP();
            } else {
                Options["--remux-video"] = targetpath.getExt();
                Options["--output"] = targetpath.QP();
            }
            return this;
        }
        public DLP DownloadAudio(string audio_id, string target) {
            Options["--extract-audio"] = "";
            if (target.isAudio()) {
                Options["--audio-format"] = target.getExt().QS();
            }
            Options["--format"] = audio_id;
            Options["--output"] = target.RemoveExt().QP();
            return this;
        }
        public DLP DownloadSubtitle(string? lang, string targetpath) { // lang is now string?
            //Default
            if (string.IsNullOrWhiteSpace(lang)) return this; // Ensure lang is not null or whitespace for --sub-langs
            if (!Path.HasExtension(targetpath)) Path.ChangeExtension(targetpath, ".srt");
            var exts = Path.GetExtension(targetpath).Trim('.').ToLower();
            Options["--write-subs"] = "";
            Options["--sub-langs"] = lang;
            if (new[] { "vtt", "ttml", "srv3", "srv2", "srv1", "json3" }.Contains(exts)) {
                Options["--sub-format"] = exts;
            } else if (new[] { "ass", "lrc", "srt" }.Contains(exts)) {
                Options["--convert-subs"] = exts;
            }
            var subpath = Path.ChangeExtension(targetpath, null);
            Options["--skip-download"] = "";
            Options["[subtitle]"] = subpath.QP("subtitle");
            return this;
        }
        private static Regex ErrSign = new Regex(@"^(?=.*?ERROR)(?=.*?sign)(?=.*?confirm)", RegexOptions.IgnoreCase);
        private static Regex ErrUnsupported = new Regex(@"^(?=.*?ERROR)(?=.*?Unsupported)", RegexOptions.IgnoreCase);

        // Ensure using yt_dlp_gui.Models; is at the top if not already present

        public static async Task EnsureYtdlpPathAsync() {
            if (isYtdlpPathEnsured && File.Exists(Path_DLP)) {
                Debug.WriteLine("yt-dlp path already ensured and file exists.");
                return;
            }

            try {
                Debug.WriteLine("Ensuring yt-dlp path...");
                string? newPath = await YtdlpUpdater.CheckAndUpdateYtdlpAsync();
                if (!string.IsNullOrEmpty(newPath) && File.Exists(newPath)) {
                    Path_DLP = newPath;
                    isYtdlpPathEnsured = true;
                    Debug.WriteLine($"yt-dlp path set to: {Path_DLP}");
                } else {
                    Debug.WriteLine("Failed to get a valid path from YtdlpUpdater or file does not exist.");
                    // Optionally, clear Path_DLP or isYtdlpPathEnsured if update fails
                    // isYtdlpPathEnsured = false;
                }
            } catch (Exception ex) {
                Debug.WriteLine($"Error ensuring yt-dlp path: {ex.Message}");
                // isYtdlpPathEnsured = false;
            }
        }

        public Process? Exec(DownloadItem? itemToUpdate,
                                Action<DownloadItem?, string>? stdall = null,
                                Action<DownloadItem?, string>? stdout = null,
                                Action<DownloadItem?, string>? stderr = null) {
            if (!isYtdlpPathEnsured || string.IsNullOrEmpty(Path_DLP) || !File.Exists(Path_DLP)) {
                throw new InvalidOperationException("yt-dlp path is not configured or yt-dlp.exe is missing. EnsureYtdlpPathAsync must be called and complete successfully before execution.");
            }

            // Apply proxy settings from Config.Default
            if (Config.Default == null) {
                System.Diagnostics.Debug.WriteLine("DLP.Exec: Config.Default is null. Attempting to load.");
                Config.Load(); // Ensure config is loaded
                if (Config.Default == null) { // Still null after load, create new
                    System.Diagnostics.Debug.WriteLine("DLP.Exec: Config.Default is still null after Load. Creating new Config instance for Default.");
                    Config.Default = new Config();
                }
            }

            // Apply Audio-Only Download settings from Config.Default
            if (Config.Default != null) { // Ensure Config.Default is not null
                if (Config.Default.DownloadAudioOnly) {
                    Options["--extract-audio"] = ""; // Short form is -x
                    if (!string.IsNullOrWhiteSpace(Config.Default.PreferredAudioFormat)) {
                        Options["--audio-format"] = Config.Default.PreferredAudioFormat;
                        System.Diagnostics.Debug.WriteLine($"DLP.Exec: Audio-Only enabled. Format: {Config.Default.PreferredAudioFormat}");
                    } else {
                        // If audio format is somehow empty but audio-only is checked, remove specific audio format option
                        // yt-dlp will then use a default audio format for extraction.
                        Options.Remove("--audio-format");
                        System.Diagnostics.Debug.WriteLine("DLP.Exec: Audio-Only enabled. No specific audio format preferred, yt-dlp will use default.");
                    }
                } else {
                    // Ensure audio-only options are removed if not enabled
                    Options.Remove("--extract-audio"); // or -x
                    Options.Remove("--audio-format");
                }
            }

            // Apply Video Format setting from Config.Default
            if (Config.Default != null && !string.IsNullOrWhiteSpace(Config.Default.PreferredVideoFormat)) {
                Options["--format"] = Config.Default.PreferredVideoFormat; // yt-dlp format strings are typically not quoted with QS() unless they contain shell special chars, direct assignment is common.
                System.Diagnostics.Debug.WriteLine($"DLP.Exec: Using video format: {Options["--format"]}");
            } else {
                // If no preferred format is set or it's empty, remove any lingering --format option
                // This ensures yt-dlp uses its default behavior if the user clears the setting.
                if (Options.ContainsKey("--format")) {
                    Options.Remove("--format");
                    System.Diagnostics.Debug.WriteLine("DLP.Exec: No preferred video format set or format is empty. Removed --format option.");
                } else {
                    System.Diagnostics.Debug.WriteLine("DLP.Exec: No preferred video format set. yt-dlp will use its default.");
                }
            }

            if (Config.Default != null && Config.Default.ProxyEnabled && !string.IsNullOrWhiteSpace(Config.Default.ProxyUrl) && !string.IsNullOrWhiteSpace(Config.Default.ProxyPort)) {
                string proxyUrl = Config.Default.ProxyUrl;
                string proxyPort = Config.Default.ProxyPort;
                string proxyUser = Config.Default.ProxyUsername;
                string proxyPass = Config.Default.ProxyPassword;
                string fullProxyString = proxyUrl; // Assume URL might contain scheme like http:// or socks5://

                // Attempt to form a complete proxy string: scheme://[user:pass@]host:port
                // Basic assembly, assuming ProxyUrl is like "scheme://host" or just "host"
                string scheme = "";
                string host = proxyUrl;

                if (proxyUrl.Contains("://")) {
                    scheme = proxyUrl.Substring(0, proxyUrl.IndexOf("://") + 3);
                    host = proxyUrl.Substring(proxyUrl.IndexOf("://") + 3);
                }

                // Remove port from host if it's already there to avoid host:port:port
                if (host.Contains(":")) {
                    host = host.Substring(0, host.IndexOf(":"));
                }

                if (!string.IsNullOrWhiteSpace(proxyUser) && !string.IsNullOrWhiteSpace(proxyPass)) {
                    fullProxyString = $"{scheme}{proxyUser}:{proxyPass}@{host}:{proxyPort}";
                } else {
                    fullProxyString = $"{scheme}{host}:{proxyPort}";
                }

                Options["--proxy"] = fullProxyString.QS(); // .QS() is an existing extension method
                System.Diagnostics.Debug.WriteLine($"DLP.Exec: Using proxy: {Options["--proxy"]}");

                // Handle Aria2c proxy args
                if (Options.ContainsKey("--external-downloader") && Options["--external-downloader"].Contains("aria2c")) {
                    Options["--external-downloader-args"] = $"'--all-proxy={fullProxyString}'";
                    System.Diagnostics.Debug.WriteLine($"DLP.Exec: Using Aria2c proxy args: {Options["--external-downloader-args"]}");
                }
            } else {
                // Proxy is not enabled or not fully configured, ensure it's removed from options
                Options.Remove("--proxy");
                if (Options.ContainsKey("--external-downloader-args") && Options["--external-downloader-args"].Contains("--all-proxy")) {
                    Options.Remove("--external-downloader-args");
                    System.Diagnostics.Debug.WriteLine($"DLP.Exec: Proxy disabled or misconfigured. Removed proxy options.");
                }
            }

            var fn = Path_DLP;
            // It's already checked above, but keeping this as a safeguard, though technically redundant now.
            if (!File.Exists(fn)) {
                // This specific return null might be unreachable if the exception is thrown first.
                // Consider if this path is still needed or if the exception is the sole guard.
                return null;
            }
            var info = new ProcessStartInfo() {
                FileName = fn,
                Arguments = Args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            //Debug.WriteLine(Args);
            Debug.WriteLine($"{info.FileName} {info.Arguments}");
            process.StartInfo = info;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (s, e) => {
                Debug.WriteLine(e.Data, "STD");
                if (!string.IsNullOrWhiteSpace(e.Data)) {
                    stdall?.Invoke(itemToUpdate, e.Data);
                    stdout?.Invoke(itemToUpdate, e.Data);
                }
            };
            process.ErrorDataReceived += (s, e) => {
                Debug.WriteLine(e.Data, "ERR");
                if (!string.IsNullOrWhiteSpace(e.Data)) {
                    stdall?.Invoke(itemToUpdate, e.Data);
                    stderr?.Invoke(itemToUpdate, e.Data);
                    if (ErrSign.IsMatch(e.Data)) {
                        StdErr.Add(DLPError.Sign);
                    }
                    if (ErrUnsupported.IsMatch(e.Data)) StdErr.Add(DLPError.Unsupported);
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return process;
        }
        public DLP Close() {
            Debug.WriteLine("CLOSE");
            Util.SendCtrlC(process);
            if (Options.ContainsKey("--output")) {
                var tempfile = Options["--output"];
                if (!IsLive) {
                    if (File.Exists(tempfile)) File.Delete(tempfile);
                }
            }
            return this;
        }
        public DLP Err(DLPError err, Action callback) {
            if (StdErr.Contains(err)) callback.Invoke();

            return this;
        }
    }
    public static class DLPExtend {
        public static string QS(this string str) {
            return $"\"{str}\"";
        }
        public static string QP(this string path, string prefix = "") {
            var p = path.Replace(Path.DirectorySeparatorChar, '/');
            if (string.IsNullOrWhiteSpace(prefix)) {
                return p.QS();
            } else {
                return $"{prefix}:{p}".QS();
            }
        }
        public static string RemoveExt(this string path) {
            if (path.isVideo() ||
                path.isAudio() ||
                path.isImage() ||
                path.isSubtitle()) {
                return Path.ChangeExtension(path, null);
            } else {
                return path;
            }
        }
        public static bool isVideo(this string path) {
            if (!Path.HasExtension(path)) return false;
            var exts = new[] { "avi", "flv", "mkv", "mov", "mp4", "webm" };
            return exts.Contains(Path.GetExtension(path).ToLower().Trim('.'));
        }
        public static bool isAudio(this string path) {
            if (!Path.HasExtension(path)) return false;
            var exts = new[] { "aac", "aiff", "alac", "flac", "m4a", "mka", "mp3", "ogg", "opus", "vorbis", "wav" };
            return exts.Contains(Path.GetExtension(path).ToLower().Trim('.'));
        }
        public static bool isImage(this string path) {
            if (!Path.HasExtension(path)) return false;
            var exts = new[] { "gif", "jpg", "png", "webp" };
            return exts.Contains(Path.GetExtension(path).ToLower().Trim('.'));
        }
        public static bool isSubtitle(this string path) {
            if (!Path.HasExtension(path)) return false;
            var exts = new[] { "vtt", "ttml", "srv3", "srv2", "srv1", "json3" };
            return exts.Contains(Path.GetExtension(path).ToLower().Trim('.'));
        }
        public static string getExt(this string path) {
            return Path.GetExtension(path).Trim('.').ToLower();
        }
        public static string suffix(this string path, string suffix) {
            var oriExt = Path.GetExtension(path);
            return Path.ChangeExtension(Path.ChangeExtension(path, null) + suffix, oriExt);
        }
    }
}
