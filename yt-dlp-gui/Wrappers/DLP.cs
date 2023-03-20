using Libs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using yt_dlp_gui.Models;

namespace yt_dlp_gui.Wrappers {

    public class DLP {
        public enum DLPType {
            yd_dlp, youtube_dl
        }
        static public DLPType Type { get; set; } = DLPType.yd_dlp;
        static public string Path_DLP { get; set; } = string.Empty;
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
        public DLP SplitChapters(Chapters chapters, string path = "") {
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
        public DLP Subtitle(string lang, string targetpath, bool embed) {
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
                            key = "--paths";
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
        public DLP DownloadFormat(string format_id, string targetpath) {
            Debug.WriteLine($"id:{format_id} path:{targetpath}", "DownloadFormat");
            Options["--format"] = format_id;
            Options["--remux-video"] = targetpath.getExt();
            Options["--output"] = Path.ChangeExtension(targetpath, ".%(ext)s").QP();
            Files.Add(targetpath);
            return this;
        }
        public DLP DownloadVideo(string format_id, string source_ext, string targetpath) {
            Debug.WriteLine($"id:{format_id} source:{source_ext} path:{targetpath}", "DownloadVideo");
            Options["--format"] = format_id;
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
        public DLP DownloadSubtitle(string lang, string targetpath) {
            //Default
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
        public Process Exec(Action<string> stdall = null, Action<string> stdout = null, Action<string> stderr = null) {
            //var fn = App.Path(App.Folders.bin, "yt-dlp.exe");
            var fn = Path_DLP;
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
                    stdall?.Invoke(e.Data);
                    stdout?.Invoke(e.Data);
                }
            };
            process.ErrorDataReceived += (s, e) => {
                Debug.WriteLine(e.Data, "ERR");
                if (!string.IsNullOrWhiteSpace(e.Data)) {
                    stdall?.Invoke(e.Data);
                    stderr?.Invoke(e.Data);
                    if (ErrSign.IsMatch(e.Data)) StdErr.Add(DLPError.Sign);
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
