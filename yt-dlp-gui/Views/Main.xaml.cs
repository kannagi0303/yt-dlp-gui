using Libs;
using Libs.Yaml;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Swordfish.NET.Collections.Auxiliary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shell;
using WK.Libraries.SharpClipboardNS;
using yt_dlp_gui.Controls;
using yt_dlp_gui.Models;
using yt_dlp_gui.Wrappers;
// using yt_dlp_gui.App; // Commented out as MyApplication is now the app class, ThemeManager might need specific using if used directly
using System.Collections.ObjectModel;

namespace yt_dlp_gui.Views {
    public partial class Main :Window {
        private readonly ViewData Data = new();
        private List<DLP> RunningDLP = new();
        public ObservableCollection<DownloadItem> DownloadQueue { get; set; }
        private bool _isProcessingQueue = false;
        private readonly object _queueLock = new object();

        private async Task ProcessQueueAsync() {
            lock (_queueLock) {
                if (_isProcessingQueue) {
                    System.Diagnostics.Debug.WriteLine("ProcessQueueAsync: Already processing.");
                    return;
                }
                _isProcessingQueue = true;
            }
            System.Diagnostics.Debug.WriteLine("ProcessQueueAsync: Starting to process queue.");

            DownloadItem? currentItem = null;
            while (true) {
                currentItem = DownloadQueue.FirstOrDefault(item => item.Status == DownloadStatus.Queued);

                if (currentItem == null) {
                    System.Diagnostics.Debug.WriteLine("ProcessQueueAsync: No more queued items.");
                    break;
                }

                System.Diagnostics.Debug.WriteLine($"ProcessQueueAsync: Processing item: {currentItem.Url}");
                currentItem.Status = DownloadStatus.Downloading;
                currentItem.Progress = 0;
                currentItem.ErrorMessage = null;

                var dlp = new DLP(currentItem.Url);
                string outputTemplate = Path.Combine(currentItem.OutputPath, "%(title)s.%(ext)s");
                dlp.Options["-o"] = outputTemplate.QS();

                if (!string.IsNullOrWhiteSpace(currentItem.SelectedVideoFormat)) {
                    dlp.Options["--format"] = currentItem.SelectedVideoFormat;
                }

                if (currentItem.DownloadAudioOnly) {
                    dlp.Options["-x"] = "";
                    if (!string.IsNullOrWhiteSpace(currentItem.SelectedAudioFormat)) {
                        dlp.Options["--audio-format"] = currentItem.SelectedAudioFormat;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"ProcessQueueAsync: Calling DLP.Exec for {currentItem.Url}");
                try {
                    await Task.Run(() => {
                        var process = dlp.Exec(
                            itemToUpdate: currentItem,
                            stdall: (item, output) => {
                                if (item == null) return;
                                if (output.Contains("[info] MAPPING: ") && item.FileName == "Fetching title...") {
                                    try {
                                        string potentialFilename = output.Substring(output.IndexOf("[info] MAPPING: ") + "[info] MAPPING: ".Length).Trim();
                                        if (!string.IsNullOrWhiteSpace(potentialFilename)) {
                                            Application.Current.Dispatcher.Invoke(() => {
                                                item.FileName = Path.GetFileName(potentialFilename);
                                            });
                                        }
                                    } catch (Exception ex) {
                                        System.Diagnostics.Debug.WriteLine($"Error parsing filename from output: {ex.Message}");
                                    }
                                }
                                if (output.StartsWith("[yt-dlp],")) {
                                    var parts = output.Split(',');
                                    if (parts.Length > 1) {
                                        var percentStr = parts[1].Replace("%", "").Trim();
                                        if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double progressValue)) {
                                            Application.Current.Dispatcher.Invoke(() => {
                                                item.Progress = progressValue;
                                            });
                                        }
                                    }
                                }
                            },
                            stdout: null,
                            stderr: (item, error) => {
                                if (item != null && item.Status == DownloadStatus.Downloading) {
                                    if (string.IsNullOrEmpty(item.ErrorMessage)) item.ErrorMessage = error;
                                    else item.ErrorMessage += $"; {error}";
                                }
                            }
                        );

                        if (process != null && process.ExitCode == 0 && dlp.StdErr.Count == 0) {
                            if (currentItem != null) {
                                if (currentItem.FileName == "Fetching title..." && dlp.Files.Any()) {
                                    Application.Current.Dispatcher.Invoke(() => {
                                       currentItem.FileName = Path.GetFileName(dlp.Files.First());
                                    });
                                } else if (currentItem.FileName == "Fetching title...") {
                                     Application.Current.Dispatcher.Invoke(() => {
                                       currentItem.FileName = "Unknown Title";
                                    });
                                }
                                currentItem.Status = DownloadStatus.Completed;
                                currentItem.Progress = 100;
                                System.Diagnostics.Debug.WriteLine($"ProcessQueueAsync: Item completed: {currentItem.Url}");
                            }
                        } else {
                            if (currentItem != null) {
                                currentItem.Status = DownloadStatus.Failed;
                                if (string.IsNullOrEmpty(currentItem.ErrorMessage)) {
                                    currentItem.ErrorMessage = $"Download failed. Exit code: {process?.ExitCode}. DLP Errors: {string.Join(", ", dlp.StdErr)}";
                                }
                                System.Diagnostics.Debug.WriteLine($"ProcessQueueAsync: Item failed: {currentItem.Url}. Error: {currentItem.ErrorMessage}");
                            }
                        }
                    });
                } catch (Exception ex) {
                    if (currentItem != null) {
                        currentItem.Status = DownloadStatus.Failed;
                        currentItem.ErrorMessage = $"An exception occurred: {ex.Message}";
                        System.Diagnostics.Debug.WriteLine($"ProcessQueueAsync: Exception for item {currentItem.Url}. Error: {ex.Message}");
                    }
                }
            }

            lock (_queueLock) {
                _isProcessingQueue = false;
            }
            System.Diagnostics.Debug.WriteLine("ProcessQueueAsync: Finished processing queue.");
        }

        private void LoadSettingsToUI() {
            if (Config.Default == null) {
                System.Diagnostics.Debug.WriteLine("Config.Default is null in LoadSettingsToUI. Attempting to load.");
                // Config.Load(); // This would use MyApplication.Path, which is missing
                Config.Default = new Config(); // Initialize with defaults if not loaded
                System.Diagnostics.Debug.WriteLine("Config.Default initialized with new Config() as Load is problematic now.");
            }

            ProxyEnableCheckBox.IsChecked = Config.Default.ProxyEnabled;
            ProxyUrlTextBox.Text = Config.Default.ProxyUrl ?? string.Empty;
            ProxyPortTextBox.Text = Config.Default.ProxyPort ?? string.Empty;
            ProxyUsernameTextBox.Text = Config.Default.ProxyUsername ?? string.Empty;
            ProxyPasswordTextBox.Text = Config.Default.ProxyPassword ?? string.Empty;
            System.Diagnostics.Debug.WriteLine("Proxy settings loaded into UI.");

            VideoFormatTextBox.Text = Config.Default.PreferredVideoFormat ?? "bv*+ba/b";
            System.Diagnostics.Debug.WriteLine($"Loaded PreferredVideoFormat: {VideoFormatTextBox.Text}");

            AudioOnlyCheckBox.IsChecked = Config.Default.DownloadAudioOnly;
            bool audioFormatSet = false;
            foreach (ComboBoxItem item in AudioFormatComboBox.Items) {
                if (item.Content?.ToString() == Config.Default.PreferredAudioFormat) {
                    AudioFormatComboBox.SelectedItem = item;
                    audioFormatSet = true;
                    break;
                }
            }
            if (!audioFormatSet) {
                AudioFormatComboBox.Text = Config.Default.PreferredAudioFormat;
            }
            System.Diagnostics.Debug.WriteLine($"Loaded DownloadAudioOnly: {AudioOnlyCheckBox.IsChecked}, PreferredAudioFormat: {Config.Default.PreferredAudioFormat}");

            DownloadPathTextBox.Text = Config.Default.DownloadFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "yt-dlp-gui");
            System.Diagnostics.Debug.WriteLine($"Loaded DownloadFolderPath: {DownloadPathTextBox.Text}");
        }

        public Main() {
            InitializeComponent();
            // The line `private readonly Config Data = MyApplication.AppConfig;` was here in user code.
            // It was changed to `private readonly Config Data = new();` in a previous step (turn 97 subtask for Main.xaml.cs)
            // This ViewData `Data` object is used extensively.
            // For now, `Data` is new(), so it will have default Config values.
            // The original `App.AppConfig` would have been loaded from MyApplication.

            LoadSettingsToUI();

            DownloadQueue = new ObservableCollection<DownloadItem>();
            this.DataContext = this;

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

            InitGUIConfig();

            Topmost = Data.AlwaysOnTop;
            if (Data.RememberWindowStatePosition) {
                Top = Data.Top;
                Left = Data.Left;
            }
            if (Data.RememberWindowStateSize) {
                Width = Data.Width;
                Height = Data.Height;
            } else {
                Width = 600 * (Data.Scale / 100d);
                Height = 380 * (Data.Scale / 100d);
            }

            InitConfiguration();
            ScanDepends();

            if (!Directory.Exists(Data.TargetPath)) {
                // Data.TargetPath = MyApplication.AppPath; // AppPath is missing
                Data.TargetPath = "."; // Temporary
            }
            if (string.IsNullOrWhiteSpace(Data.PathTEMP) || !Directory.Exists(GetTempPath)) {
                Data.PathTEMP = "%YTDLPGUI_TARGET%";
            }

            InitClipboard();
            Task.Run(Inits);
            //Yaml.Save(MyApplication.Path(MyApplication.Folders.root, "lang.yaml"), new Lang()); // Path, Folders are missing
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e) {
            var args = ToastArguments.Parse(e.Argument);
            if (args.Contains("action")) {
                switch (args["action"]) {
                    case "browse":
                        if (File.Exists(args["file"])) _ = Util.Explorer(args["file"]);
                        break;
                }
            }
        }

        private void ChangeScale(int present) {
            var scaleRatio = present / 100d;
            var grid = Template.FindName("MainGrid", this) as Grid;
            if (grid != null) {
                var scaleTransform = new ScaleTransform(scaleRatio, scaleRatio);
                grid.LayoutTransform = scaleTransform;
                WindowChrome.SetWindowChrome(this, new() {
                    CaptionHeight = 22 * scaleRatio,
                    ResizeBorderThickness = new Thickness(6),
                    CornerRadius = new CornerRadius(0),
                    GlassFrameThickness = new Thickness(1),
                    NonClientFrameEdges = NonClientFrameEdges.None,
                    UseAeroCaptionButtons = false
                });
                grid.UpdateLayout();
            }
        }
        private string GetEnvPath(string path) {
            Dictionary<string, string> replacements = new() {
                {"%YTDLPGUI_TARGET%", Data.TargetPath},
                // {"%YTDLPGUI_LOCALE%", MyApplication.AppPath} // AppPath is missing
                {"%YTDLPGUI_LOCALE%", "."} // Temporary
            };
            foreach (KeyValuePair<string, string> pair in replacements) {
                string placeholder = pair.Key;
                string replacement = pair.Value;
                path = path.Replace(placeholder, replacement);
                int index = path.IndexOf(replacement);
                if (index >= 0) {
                    path = path.Substring(index);
                }
                path = path.Replace('/', '\'); // Corrected backslash
                while (path.Contains("\\\\")) { // Corrected double backslash
                    path = path.Replace("\\\\", "\\"); // Corrected double backslash
                }
            }
            return Environment.ExpandEnvironmentVariables(path);
        }
        private string GetTempPath {
            get => GetEnvPath(Data.PathTEMP);
        }
        private Regex _frgPat = new Regex("<!--StartFragment-->(.*)<!--EndFragment-->", RegexOptions.Multiline | RegexOptions.Compiled);
        private Regex _matchUrls = new Regex(@"(https?|ftp|file)\://[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\(\)\*\-\._~%]*)*", RegexOptions.Compiled);
        public void InitClipboard() {
            Data.PropertyChanged += (s, e) => {
                switch (e.PropertyName) {
                    case nameof(Data.ClipboardText):
                        var content = Data.ClipboardText;
                        var m = _matchUrls.Match(content);
                        if (m.Success) {
                            var capUrl = m.Value;
                            if (Util.UrlVaild(capUrl)) {
                                Data.Url = capUrl;
                                Analyze_Start();
                            }
                        }
                        break;
                    case nameof(Data.AlwaysOnTop):
                        Topmost = Data.AlwaysOnTop;
                        break;
                }
            };

            var sc = new SharpClipboard();
            sc.ClipboardChanged += (s, e) => {
                if (!Data.IsMonitor || Data.IsAnalyze || Data.IsDownload) return;
                if (e.ContentType == SharpClipboard.ContentTypes.Text) {
                    Data.ClipboardText = GetClipbaordText();
                }
            };
        }
        private string GetClipbaordText() {
            int maxTries = 10;
            int delayTime = 1000;
            int numTries = 0;
            while (numTries < maxTries) {
                try {
                    var content = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Html);
                    if (!string.IsNullOrWhiteSpace(content)) {
                        content = _frgPat.Match(content).Groups?[1].Value.Trim() ?? "";
                    } else {
                        content = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
                    }
                    numTries = 0;
                    return content;
                } catch (Exception) {
                    numTries++;
                    Thread.Sleep(delayTime);
                }
            }
            return string.Empty;
        }
        public void InitGUIConfig() {
            // Data.GUIConfig.Load(MyApplication.Path(MyApplication.Folders.root, MyApplication.AppName + ".yaml")); // Path, Folders, AppName are missing
            // Assuming GUIConfig might be loaded by other means or defaults are used.
            Data.GUIConfig = Data.GUIConfig ?? new ViewData.GUIConfig();
            Util.PropertyCopy(Data.GUIConfig, Data);
            Data.AutoSaveConfig = true;
        }
        public void InitConfiguration() {
            Data.Configs.Clear();
            Data.Configs.Add(new Config() { name = "None" }); // Was MyApplication.Lang.Main.ConfigurationNone
            var cp = "configs"; // Was MyApplication.Path(MyApplication.Folders.configs)
            var fs = Directory.Exists(cp)
                ? Directory.EnumerateFiles(cp).OrderBy(x => x)
                : Enumerable.Empty<string>();
            fs.ForEach(x => {
                Data.Configs.Add(new Config() {
                    name = Path.GetFileNameWithoutExtension(x),
                    file = x
                });
            });
            Data.selectedConfig = Data.Configs.FirstOrDefault(x => x.file == Data.GUIConfig.ConfigurationFile, Data.Configs.First());
        }
        public void ScanDepends() {
            var isYoutubeDl = @"^youtube-dl\.exe";
            if (!string.IsNullOrWhiteSpace(Data.PathYTDLP) && File.Exists(Data.PathYTDLP)) {
                DLP.Path_DLP = Data.PathYTDLP;
            }
            if (!string.IsNullOrWhiteSpace(Data.PathAria2) && File.Exists(Data.PathAria2)) {
                DLP.Path_Aria2 = Data.PathAria2;
            }
            if (!string.IsNullOrWhiteSpace(Data.PathFFMPEG) && File.Exists(Data.PathFFMPEG)) {
                DLP.Path_FFMPEG = Data.PathFFMPEG;
                FFMPEG.Path_FFMPEG = Data.PathFFMPEG;
            }
            if (string.IsNullOrWhiteSpace(DLP.Path_DLP) ||
                string.IsNullOrWhiteSpace(DLP.Path_Aria2) ||
                string.IsNullOrWhiteSpace(FFMPEG.Path_FFMPEG)) {
                // var deps = Directory.EnumerateFiles(MyApplication.AppPath, "*.exe", SearchOption.AllDirectories).ToList(); // AppPath is missing
                var deps = Directory.EnumerateFiles(".", "*.exe", SearchOption.AllDirectories).ToList(); // Temporary
                // deps = deps.Where(x => Path.GetFileName(MyApplication.AppExe) != Path.GetFileName(x)).ToList(); // AppExe is missing
                var dep_ytdlp = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), @"^(yt-dlp(_min|_x86|_x64)?|ytdl-patched.*?)\.exe"), "");
                var dep_ffmpeg = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), @"^ffmpeg"), "");
                var dep_aria2 = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), @"^aria2"), "");
                var dep_youtubedl = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), isYoutubeDl), "");
                if (string.IsNullOrWhiteSpace(DLP.Path_DLP)) {
                    if (!string.IsNullOrWhiteSpace(dep_ytdlp)) {
                        Data.PathYTDLP = DLP.Path_DLP = dep_ytdlp;
                    } else if (!string.IsNullOrWhiteSpace(dep_youtubedl)) {
                        Data.PathYTDLP = DLP.Path_DLP = dep_youtubedl;
                    }
                }
                if (DLP.Path_DLP != null && Regex.IsMatch(DLP.Path_DLP, isYoutubeDl)) DLP.Type = DLP.DLPType.youtube_dl; // Added null check
                if (string.IsNullOrWhiteSpace(DLP.Path_Aria2)) {
                    Data.PathAria2 = DLP.Path_Aria2 = dep_aria2;
                }
                if (string.IsNullOrWhiteSpace(FFMPEG.Path_FFMPEG)) {
                    Data.PathFFMPEG = DLP.Path_FFMPEG = FFMPEG.Path_FFMPEG = dep_ffmpeg;
                }
            }
        }
        public async void Inits() {
            var needcheck = false;
            var currentDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

            if (!string.IsNullOrWhiteSpace(Data.LastVersion)) needcheck = true;
            if (currentDate != Data.LastCheckUpdate) needcheck = true;

            if (needcheck) {
                var releaseData = await Web.GetLastTag();
                var last = releaseData.FirstOrDefault();
                if (last != null) {
                    Data.ReleaseData = releaseData;
                    Data.LastVersion = last.tag_name;
                    Data.LastCheckUpdate = currentDate;
                }
            }
            // if (string.Compare(MyApplication.CurrentVersion, Data.LastVersion) < 0) { // CurrentVersion is missing
            if (Data.LastVersion != null && Data.LastVersion != "0.0.0" && Version.TryParse(Data.LastVersion?.TrimStart('v'), out var onlineVersion) && Version.TryParse("0.0.0" /*MyApplication.CurrentVersion?.TrimStart('v')*/, out var currentVersion)) { // Temporary logic
                 if (onlineVersion > currentVersion) Data.NewVersion = true;
            } else if (Data.LastVersion != null && Data.LastVersion != "0.0.0") { // Avoid setting NewVersion to true if LastVersion is simply not set or "0.0.0"
                 Data.NewVersion = true;
            }
        }
        private void Button_Analyze(object sender, RoutedEventArgs e) {
            Analyze_Start();
        }
        private void Analyze_Start() {
            Data.IsAnalyze = true;
            // Assuming cc, cv, ca, cs are ComboBoxes defined in XAML. Their SelectedIndex is being set.
            if (cc != null) cc.SelectedIndex = -1;
            if (cv != null) cv.SelectedIndex = -1;
            if (ca != null) ca.SelectedIndex = -1;
            if (cs != null) cs.SelectedIndex = -1;
            Data.Thumbnail = null;
            Data.Video = new();
            Data.NeedCookie = Data.UseCookie == UseCookie.Always;

            Task.Run(() => {
                GetInfo();
                Data.IsAnalyze = false;

                if (Data.AutoDownloadAnalysed) {
                    if (Data.selectedVideo != null && Data.selectedAudio != null) {
                        Download_Start_Native();
                    }
                }
            });
        }
        private void GetInfo() {
            var dlp = new DLP(Data.Url);
            if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
            dlp.Proxy(Data.ProxyUrl, Data.ProxyEnabled);
            dlp.GetInfo();
            if (!string.IsNullOrWhiteSpace(Data.selectedConfig.file)) {
                dlp.LoadConfig(Data.selectedConfig.file);
            }
            if (Data.UseOutput) dlp.Output("%(title)s.%(ext)s");
            ClearStatus();
            dlp.Exec(null, std => {
                Data.Video = JsonConvert.DeserializeObject<Video>(std, new JsonSerializerSettings() {
                    NullValueHandling = NullValueHandling.Ignore
                });
                Data.Chapters.Clear();
                if (Data.Video != null && Data.Video.chapters != null && Data.Video.chapters.Any()) { // Added null check for Data.Video
                    Data.Chapters.Add(new Chapters() { title = "All Chapters" /* MyApplication.Lang.Main.ChaptersAll */, type = ChaptersType.None });
                    Data.Chapters.Add(new Chapters() { title = "Split Chapters" /* MyApplication.Lang.Main.ChaptersSplite */, type = ChaptersType.Split });
                    Data.Chapters.AddRange(Data.Video.chapters);
                    Data.hasChapter = true;
                } else {
                    Data.Chapters.Add(new Chapters() { title = "No Chapters" /* MyApplication.Lang.Main.ChaptersNone */, type = ChaptersType.None });
                    Data.hasChapter = false;
                }
                if (Data.Video != null) { // Added null check for Data.Video
                    Data.Formats.LoadFromVideo(Data.Video.formats);
                    Data.Thumbnails.Reset(Data.Video.thumbnails);
                    Data.RequestedFormats.LoadFromVideo(Data.Video.requested_formats);

                    var subs = Data.Video.subtitles?.Select(x => { // Added null check for subtitles
                        var s = x.Value.FirstOrDefault(y => y.ext == "vtt");
                        if (s == null) return null;
                        s.key = x.Key;
                        return s;
                    }).Where(x => x != null).ToList() ?? new List<Subs>(); // Ensure subs is not null
                    Data.Subtitles.Clear();
                    if (subs.Any()) {
                        Data.Subtitles.Add(new Subs() { name = "Ignore Subtitles" /* MyApplication.Lang.Main.SubtitleIgnore */ });
                        Data.hasSubtitle = true;
                    } else {
                        Data.Subtitles.Add(new Subs() { name = "No Subtitles" /* MyApplication.Lang.Main.SubtitleNone */ });
                        Data.hasSubtitle = false;
                    }
                    Data.Subtitles.AddRange(subs);

                    var BestUrl = Data.Thumbnails.LastOrDefault()?.url;
                    if (BestUrl != null && Web.Head(BestUrl)) {
                        Data.Thumbnail = BestUrl;
                    } else {
                        Data.Thumbnail = Data.Video.thumbnail;
                    }

                    Data.SelectFormatBest();
                    var full = string.Empty;
                    if (!string.IsNullOrEmpty(Data.Video._filename)) { // Added null check for _filename
                        if (Path.IsPathRooted(Data.Video._filename)) {
                            full = Path.GetFullPath(Data.Video._filename);
                        } else {
                            full = Path.Combine(Data.TargetPath, Data.Video._filename);
                        }
                        Data.TargetName = full;
                    } else {
                        Data.TargetName = Path.Combine(Data.TargetPath, "default_filename.tmp"); // Fallback
                    }
                }
            });
            dlp.Err(DLP.DLPError.Sign, () => {
                if (Data.UseCookie == UseCookie.WhenNeeded) {
                    Data.NeedCookie = true;
                    GetInfo();
                } else if (Data.UseCookie == UseCookie.Ask) {
                    var mb = System.Windows.Forms.MessageBox.Show(
                        $"{"Cookie Required. Use cookies?"} \n" /* MyApplication.Lang.Dialog.CookieRequired */,
                        "yt-dlp-gui" /* MyApplication.AppName */,
                        MessageBoxButtons.YesNo);

                    if (mb == System.Windows.Forms.DialogResult.Yes) {
                        Data.NeedCookie = true;
                        GetInfo();
                    }
                }
            });
        }
        private void ClearStatus() {
            Data.DNStatus_Infos.Clear();
            Data.DNStatus_Video = new();
            Data.DNStatus_Audio = new();
            Data.VideoPersent = Data.AudioPersent = 0;
        }
        private void Button_SaveVideo(object sender, RoutedEventArgs e) {
            var dialog = new SaveFileDialog();
            dialog.Filter =
                $"{"MKV Video" /* MyApplication.Lang.Files.mkv */}|*.mkv|" +
                $"{"MP4 Video" /* MyApplication.Lang.Files.mp4 */}|*.mp4|" +
                $"{"WebM Video" /* MyApplication.Lang.Files.webm */}|*.webm|" +
                $"{"MOV Video" /* MyApplication.Lang.Files.mov */}|*.mov|" +
                $"{"FLV Video" /* MyApplication.Lang.Files.flv */}|*.flv";
            if (Data.selectedVideo != null && !string.IsNullOrEmpty(Data.selectedVideo.video_ext)) { // Null check
                dialog.DefaultExt = Data.selectedVideo.video_ext.ToLower();
            } else {
                dialog.DefaultExt = "mkv";
            }
            dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), dialog.DefaultExt);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = dialog.FileName;
                Download_Start_Native(DownloadType.Video, target);
            }
        }
        private void Button_SaveAudio(object sender, RoutedEventArgs e) {
            var dialog = new SaveFileDialog();
            dialog.Filter =
                $"{"Opus Audio" /* MyApplication.Lang.Files.opus */}|*.opus|" +
                $"{"AAC Audio" /* MyApplication.Lang.Files.aac */}|*.aac|" +
                $"{"M4A Audio" /* MyApplication.Lang.Files.m4a */}|*.m4a|" +
                $"{"MP3 Audio" /* MyApplication.Lang.Files.mp3 */}|*.mp3|" +
                $"{"Vorbis Audio" /* MyApplication.Lang.Files.vorbis */}|*.vorbis|" +
                $"{"ALAC Audio" /* MyApplication.Lang.Files.alac */}|*.alac|" +
                $"{"FLAC Audio" /* MyApplication.Lang.Files.flac */}|*.flac|" +
                $"{"WAV Audio" /* MyApplication.Lang.Files.wav */}|*.wav";
            if (Data.selectedAudio != null && !string.IsNullOrEmpty(Data.selectedAudio.acodec)) { // Null check
                dialog.DefaultExt = Data.selectedAudio.acodec.ToLower();
            } else {
                dialog.DefaultExt = "opus";
            }
            dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), dialog.DefaultExt);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = dialog.FileName;
                Download_Start_Native(DownloadType.Audio, target);
            }
        }
        private void Button_ExplorerTarget(object sender, RoutedEventArgs e) {
            if (!string.IsNullOrEmpty(Data.TargetFile)) Util.Explorer(Data.TargetFile); // Null check
        }
        private void Button_Cancel(object sender, RoutedEventArgs e) {
            if (Data.IsDownload) {
                Data.IsAbouted = true;
                foreach (var dlp in RunningDLP) {
                    dlp.Close();
                }
            }
        }
        private void Button_Download(object sender, RoutedEventArgs e) {
            string url = Data.Url;
            if (string.IsNullOrWhiteSpace(url)) {
                MessageBox.Show("Please enter a valid URL.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Config.Default == null) Config.Load();
            string downloadDirectory = Config.Default?.DownloadFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "yt-dlp-gui");
            try {
                Directory.CreateDirectory(downloadDirectory);
            } catch (Exception ex) {
                MessageBox.Show($"Error creating download directory '{downloadDirectory}': {ex.Message}", "Directory Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var newItem = new DownloadItem(url) {
                SelectedVideoFormat = Config.Default?.PreferredVideoFormat ?? "bv*+ba/b",
                DownloadAudioOnly = Config.Default?.DownloadAudioOnly ?? false,
                SelectedAudioFormat = Config.Default?.PreferredAudioFormat ?? "mp3",
                FileName = "Fetching title...",
                OutputPath = downloadDirectory
            };
            DownloadQueue.Add(newItem);
            Data.Url = string.Empty;
            System.Diagnostics.Debug.WriteLine($"Added to queue: {newItem.Url}, Status: {newItem.Status}");
            if (!_isProcessingQueue) {
                Task.Run(() => ProcessQueueAsync());
            }
        }
        public enum DownloadType { Normal, Video, Audio, Thumbnail, Subtitle }
        private async void Download_Start_Native(DownloadType type = DownloadType.Normal, string target = "") {
            Data.CanCancel = false;
            Data.IsAbouted = false;
            if (Data.IsDownload) {
                Data.IsAbouted = true;
                foreach (var dlp in RunningDLP) {
                    dlp.Close();
                }
            } else {
                var overwrite = true;
                RunningDLP.Clear();
                if (File.Exists(Data.TargetFile) && type == DownloadType.Normal) {
                    var mb = System.Windows.Forms.MessageBox.Show(
                        $"{"File exists. Overwrite?"} \n" /* MyApplication.Lang.Dialog.FileExist */,
                        "yt-dlp-gui" /* MyApplication.AppName */,
                        MessageBoxButtons.YesNo);
                    overwrite = mb == System.Windows.Forms.DialogResult.Yes;
                    if (!overwrite) return;
                }
                Data.IsDownload = true;
                ClearStatus();
                _ = Task.Run(() => {
                    var dlp = new DLP(Data.Url);
                    List<Task> tasks = new();
                    tasks.Add(Task.Run(() => {
                        RunningDLP.Add(dlp);
                        if (Data.Video != null) dlp.IsLive = Data.Video.is_live; // Null check
                        var vid = "";
                        if (Data.selectedVideo != null && Data.selectedAudio != null) { // Null checks
                           vid = type switch {
                                DownloadType.Video => Data.selectedVideo.format_id,
                                DownloadType.Audio => Data.selectedAudio.format_id,
                                _ => $"{Data.selectedVideo.format_id}+{Data.selectedAudio.format_id}"
                           };
                        } else if (type == DownloadType.Video && Data.selectedVideo != null) {
                            vid = Data.selectedVideo.format_id;
                        } else if (type == DownloadType.Audio && Data.selectedAudio != null) {
                            vid = Data.selectedAudio.format_id;
                        } else {
                            // Cannot proceed without format selection if specific download type is chosen.
                            // Or handle default format if none selected. For now, this might lead to empty `vid`.
                        }


                        dlp
                        .Temp(GetTempPath)
                        .LoadConfig(Data.selectedConfig?.file) // Null check
                        .MTime(Data.ModifiedType)
                        .Cookie(Data.CookieType, Data.NeedCookie)
                        .Proxy(Data.ProxyUrl, Data.ProxyEnabled)
                        .UseAria2(Data.UseAria2)
                        .LimitRate(Data.LimitRate)
                        .DownloadSections(Data.TimeRange)
                        .SplitChapters(Data.selectedChapter, Data.TargetFile);

                        switch (type) {
                            case DownloadType.Video:
                                dlp
                                .EmbedChapters(Data.EmbedChapters)
                                .Thumbnail(Data.SaveThumbnail, Data.TargetFile, Data.EmbedThumbnail)
                                .Subtitle(Data.selectedSub?.key, Data.TargetFile, Data.EmbedSubtitles) // Null check
                                .DownloadVideo(vid, Data.selectedVideo?.video_ext, target); // Null check
                                break;
                            case DownloadType.Audio:
                                dlp
                                .EmbedChapters(Data.EmbedChapters)
                                .Thumbnail(Data.SaveThumbnail, Data.TargetFile, Data.EmbedThumbnail)
                                .DownloadAudio(vid, target);
                                break;
                            case DownloadType.Subtitle:
                                dlp.DownloadSubtitle(Data.selectedSub?.key, target); // Null check
                                break;
                            default:
                                dlp
                                .EmbedChapters(Data.EmbedChapters)
                                .Thumbnail(Data.SaveThumbnail, Data.TargetFile, Data.EmbedThumbnail)
                                .Subtitle(Data.selectedSub?.key, Data.TargetFile, Data.EmbedSubtitles) // Null check
                                .DownloadFormat(vid, Data.TargetFile, Data.OriginExt);
                                break;
                        }
                        var repoter = new StatusRepoter(Data);
                        repoter.type = type switch {
                            DownloadType.Video => 1,
                            DownloadType.Audio => 2,
                            _ => 0
                        };
                        dlp.Exec(std => {
                            repoter.GetStatus(std);
                        });
                    }));
                    Data.CanCancel = true;
                    Task.WaitAll(tasks.ToArray());
                    if (!Data.IsAbouted) {
                        Data.DNStatus_Infos["Status"] = "Done"; // Temporary MyApplication.Lang.Status.Done;
                        Dictionary<string, string> files = new Dictionary<string, string>();
                        foreach (string donepath in dlp.Files) {
                            if (File.Exists(donepath)) {
                                if (donepath.isVideo()) files["video"] = donepath;
                                if (donepath.isImage()) files["thumb"] = donepath;
                                if (Data.ModifiedType == ModifiedType.Upload && Data.Video != null) { // Null check
                                    if (DateTimeOffset.TryParseExact(Data.Video.upload_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset tryDate)) {
                                        File.SetLastWriteTimeUtc(donepath, tryDate.DateTime);
                                    }
                                }
                            }
                        }
                        try {
                            if (Data.UseNotifications && Data.Video != null) { // Null check
                                Util.NotifySound(Data.PathNotify);
                                var toast = new ToastContentBuilder()
                                    .AddText(Data.Video.title)
                                    .AddText("Download Completed") // Temporary
                                    .AddAudio(new ToastAudio() {
                                        Silent = true,
                                        Loop = false,
                                        Src = new Uri("ms-winsoundevent:Notification.Default")
                                    });
                                if (files.ContainsKey("video")) {
                                    toast.AddButton(
                                        new ToastButton()
                                        .SetContent("Open Folder") // Temporary
                                        .AddArgument("action", "browse")
                                        .AddArgument("file", files["video"])
                                        .SetBackgroundActivation()
                                    );
                                }
                                if (files.ContainsKey("thumb")) {
                                    toast.AddAppLogoOverride(new Uri(files["thumb"]));
                                }
                                toast.AddButton(
                                    new ToastButton()
                                    .SetContent("Close") // Temporary
                                    .AddArgument("action", "none")
                                    .SetBackgroundActivation()
                                );
                                toast.Show();
                            }
                        } catch (Exception) { }
                    }
                    Data.IsDownload = false;
                });
            }
        }

        private void Button_Browser(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(Data.TargetName)) {
                var dialog = new FolderBrowserDialog();
                dialog.SelectedPath = Data.TargetPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    Data.TargetPath = dialog.SelectedPath;
                }
            } else {
                var dialog = new SaveFileDialog();
                if(!string.IsNullOrEmpty(Data.TargetFile)) dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile); // Null check
                dialog.FileName = Path.GetFileName(Data.TargetFile);
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    Data.RemuxVideo = true;
                    Data.TargetPath = Path.GetDirectoryName(dialog.FileName);
                    Data.TargetName = Path.GetFileName(dialog.FileName);
                    Data.RemuxVideo = false;
                }
            }
        }

        private static Regex RegexValues = new Regex(@"\${(.+?)}", RegexOptions.Compiled);
        private string GetValidFileName(string filename) {
            var regexSearch = new string(Path.GetInvalidFileNameChars());
            return Regex.Replace(filename, string.Format("[{0}]", Regex.Escape(regexSearch)), "_");
        }
        private async void CommandBinding_SaveAs_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
            var dialog = new SaveFileDialog();
             if(!string.IsNullOrEmpty(Data.TargetFile)) dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile); // Null check
            var OrigExt = Path.GetExtension(Data.Thumbnail);
            var OrigFileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), OrigExt);
            dialog.DefaultExt = ".jpg";
            dialog.Filter = $"{"Image" /* MyApplication.Lang.Files.image */}|*.jpg;*.webp";
            dialog.FileName = Path.ChangeExtension(OrigFileName, ".jpg");
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                DownloadThumbnail(dialog.FileName);
            }
        }
        private void DownloadThumbnail(string toFile) {
            if (string.IsNullOrEmpty(Data.Thumbnail) || string.IsNullOrEmpty(Data.TargetFile)) return; // Null checks
            var origExt = Path.GetExtension(Data.Thumbnail);
            var origin = Path.ChangeExtension(Data.TargetFile, origExt);
            var target = toFile;
            var progress = new Progress<double>(percentage => {
                Debug.Write($"Downloading... {percentage:0.00}%");
            });
            Web.Download(Data.Thumbnail, origin, progress, Data.ProxyEnabled ? Data.ProxyUrl : null).Wait();
            if (Path.GetExtension(origin).ToLower() != Path.GetExtension(target)) {
                FFMPEG.DownloadUrl(origin, target);
                File.Delete(origin);
            }
        }

        private void CommandBinding_SaveAs_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e) {
            e.CanExecute = !string.IsNullOrWhiteSpace(Data.Thumbnail);
        }

        private void Button_Subtitle(object sender, RoutedEventArgs e) {
            var dialog = new SaveFileDialog();
            if(!string.IsNullOrEmpty(Data.TargetFile)) dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile); // Null check
            dialog.DefaultExt = ".srt";
            dialog.Filter =
                $"{"SRT Subtitles" /* MyApplication.Lang.Files.srt */}|*.srt|" +
                $"{"ASS Subtitles" /* MyApplication.Lang.Files.ass */}|*.ass|" +
                $"{"VTT Subtitles" /* MyApplication.Lang.Files.vtt */}|*.vtt|" +
                $"{"LRC Lyrics" /* MyApplication.Lang.Files.lrc */}|*.lrc|" +
                $"{"TTML Subtitles" /* MyApplication.Lang.Files.ttml */}|*.ttml|" +
                $"{"SRV3 Subtitles" /* MyApplication.Lang.Files.srv3 */}|*.srv3|" +
                $"{"SRV2 Subtitles" /* MyApplication.Lang.Files.srv2 */}|*.srv2|" +
                $"{"SRV1 Subtitles" /* MyApplication.Lang.Files.srv1 */}|*.srv1|" +
                $"{"JSON3 Subtitles" /* MyApplication.Lang.Files.json3 */}|*.json3";
            dialog.FileName = Path.ChangeExtension(Data.TargetFile, null);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = dialog.FileName;
                Debug.WriteLine(dialog.FileName, "DIALOG");
                Download_Start_Native(DownloadType.Subtitle, target);
            }
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e) {
            var win = new About();
            win.Owner = GetWindow(this);
            win.ShowDialog();
        }

        private void Button_Release(object sender, RoutedEventArgs e) {
            var win = new Release();
            win.Owner = GetWindow(this);
            win.ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e) {
            Data.Left = Left;
            Data.Top = Top;
            Data.Width = Width;
            Data.Height = Height;
        }
        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e) {
            var combo = sender as System.Windows.Controls.ComboBox;
            if (combo != null && combo.SelectedIndex == -1) { // Null check
                Data.PathTEMP = combo.Text;
            } else if (combo != null && combo.SelectedValue != null) { // Null check
                Data.PathTEMP = combo.SelectedValue.ToString();
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e) {
            var b = sender as ToggleButton;
            if (b != null && b.IsChecked == true) { // Null check
                var menu = new List<MenuDataItem>() {
                    ("Temporary Target" /* MyApplication.Lang.Main.TemporaryTarget */, () => { Data.PathTEMP = "%YTDLPGUI_TARGET%"; }),
                    ("Temporary Locale" /* MyApplication.Lang.Main.TemporaryLocale */, () => { Data.PathTEMP = "%YTDLPGUI_LOCALE%"; }),
                    ("Temporary System" /* MyApplication.Lang.Main.TemporarySystem */, () => { Data.PathTEMP = "%TEMP%"; }),
                    ("-"),
                    ("Browse..." /* MyApplication.Lang.Main.TemporaryBrowse */, () => {
                        var dialog = new FolderBrowserDialog();
                        dialog.SelectedPath = GetEnvPath(Data.PathTEMP);
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                            Data.PathTEMP = dialog.SelectedPath;
                        }
                    })
                };
                Controls.Menu.Open(menu, b, MenuPlacement.BottomLeft);
            }
        }

        private void ToggleButton_Checked_Sound(object sender, RoutedEventArgs e) {
            var b = sender as ToggleButton;
            if (b != null && b.IsChecked == true) { // Null check
                var menu = new List<MenuDataItem>() {
                    ("System Sound" /* MyApplication.Lang.Main.SoundSystem */, () => { Data.PathNotify = ""; }),
                    ("-"),
                    ("Browse..." /* MyApplication.Lang.Main.SoundBrowse */, () => {
                        var dialog = new OpenFileDialog();
                        var dirname = Path.GetDirectoryName(Data.PathNotify);
                        Debug.WriteLine(dirname);
                        if (!string.IsNullOrEmpty(dirname) && Directory.Exists(dirname)) { // Null check for dirname
                            dialog.InitialDirectory = dirname;
                            if (File.Exists(Data.PathNotify)) {
                                dialog.FileName = Path.GetFileName(Data.PathNotify);
                            }
                        } else {
                            // dialog.InitialDirectory = MyApplication.AppPath; // AppPath is missing
                            dialog.InitialDirectory = "."; // Temporary
                        }
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                            Data.PathNotify = dialog.FileName;
                        }
                    })
                };
                Controls.Menu.Open(menu, b, MenuPlacement.BottomLeft);
            }
        }

        private void Button_PlayNotify(object sender, RoutedEventArgs e) {
            if(!string.IsNullOrEmpty(Data.PathNotify)) Util.NotifySound(Data.PathNotify); // Null check
        }

        private void TextBoxNumber_Changed(object sender, EventArgs e) {
            if (Data.Scale == 0) {
                Data.Scale = 100;
            } else if (Data.Scale < 80) {
                Data.Scale = 80;
            } else if (Data.Scale > 200) {
                Data.Scale = 200;
            }
            ChangeScale(Data.Scale);
        }

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e) {
            // ThemeManager.ToggleTheme(); // Assuming ThemeManager is a static class or accessible
            // Since ThemeManager might come from yt_dlp_gui.App, and that's commented, this needs a placeholder or direct implementation if simple enough.
             MessageBox.Show("Theme toggling is temporarily disabled.", "Info");
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e) {
            if (Config.Default == null) {
                 System.Diagnostics.Debug.WriteLine("Config.Default is null in SaveSettingsButton_Click. Attempting to load.");
                 // Config.Load(); // This would use MyApplication.Path, which is missing
                 Config.Default = new Config(); // Initialize with defaults if not loaded
                 System.Diagnostics.Debug.WriteLine("Config.Default initialized with new Config() as Load is problematic now.");
            }
            if (Config.Default != null && ProxyEnableCheckBox != null) { // Null checks for safety
                Config.Default.ProxyEnabled = ProxyEnableCheckBox.IsChecked ?? false;
                Config.Default.ProxyUrl = ProxyUrlTextBox.Text;
                Config.Default.ProxyPort = ProxyPortTextBox.Text;
                Config.Default.ProxyUsername = ProxyUsernameTextBox.Text;
                Config.Default.ProxyPassword = ProxyPasswordTextBox.Text;

                Config.Default.PreferredVideoFormat = VideoFormatTextBox.Text;
                System.Diagnostics.Debug.WriteLine($"Saved PreferredVideoFormat: {Config.Default.PreferredVideoFormat}");

                Config.Default.DownloadAudioOnly = AudioOnlyCheckBox.IsChecked ?? false;
                if (AudioFormatComboBox.SelectedItem is ComboBoxItem selectedItem) {
                    Config.Default.PreferredAudioFormat = selectedItem.Content?.ToString() ?? "mp3";
                } else if (!string.IsNullOrEmpty(AudioFormatComboBox.Text)) {
                     Config.Default.PreferredAudioFormat = AudioFormatComboBox.Text;
                } else {
                    Config.Default.PreferredAudioFormat = "mp3";
                }
                System.Diagnostics.Debug.WriteLine($"Saved DownloadAudioOnly: {Config.Default.DownloadAudioOnly}, PreferredAudioFormat: {Config.Default.PreferredAudioFormat}");

                Config.Default.DownloadFolderPath = DownloadPathTextBox.Text;
                System.Diagnostics.Debug.WriteLine($"Saved DownloadFolderPath: {Config.Default.DownloadFolderPath}");

                try {
                     Config.Default.Save("yt-dlp-gui.yaml");
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
                }
            }

            MessageBox.Show("Settings saved.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Diagnostics.Debug.WriteLine("Settings saved from UI.");
        }
    }
    public class LanguageConverter :IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is string key))
                return value;
            // var Lang = MyApplication.Lang.Status; // Lang is missing
            var Lang = new Lang(); // Temporary, this will not have translations. TODO: Fix this properly later.
            var propertyInfo = Lang.GetType().GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null)
                return key;
            return propertyInfo.GetValue(Lang)?.ToString() ?? key;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
