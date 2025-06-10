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
using yt_dlp_gui.App; // Added for ThemeManager
using System.Collections.ObjectModel; // For ObservableCollection
using System.IO; // For Path
using System.Linq; // For FirstOrDefault, Any
using System.Threading.Tasks; // For Task
using yt_dlp_gui.Wrappers; // For DLP
using System.Windows; // For Application.Current.Dispatcher
using System.Globalization; // For NumberStyles and CultureInfo


namespace yt_dlp_gui.Views {
    public partial class Main :Window {
        private readonly ViewData Data = new();
        private List<DLP> RunningDLP = new();
        public ObservableCollection<DownloadItem> DownloadQueue { get; set; }
        private bool _isProcessingQueue = false;
        private readonly object _queueLock = new object(); // For thread safety with _isProcessingQueue

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
                // Find next item to process
                currentItem = DownloadQueue.FirstOrDefault(item => item.Status == DownloadStatus.Queued);

                if (currentItem == null) {
                    System.Diagnostics.Debug.WriteLine("ProcessQueueAsync: No more queued items.");
                    break; // No more items to process
                }

                System.Diagnostics.Debug.WriteLine($"ProcessQueueAsync: Processing item: {currentItem.Url}");
                currentItem.Status = DownloadStatus.Downloading;
                currentItem.Progress = 0; // Reset progress
                currentItem.ErrorMessage = null; // Clear previous errors

                var dlp = new DLP(currentItem.Url);

                string outputTemplate = Path.Combine(currentItem.OutputPath, "%(title)s.%(ext)s");
                // Ensure the output template is quoted properly if it contains spaces.
                // The .QS() extension method should handle this.
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
                            itemToUpdate: currentItem, // Pass the current item
                            stdall: (item, output) => {
                                if (item == null) return;

                                // Attempt to parse filename (existing logic)
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

                                // Attempt to parse progress
                                // Format: "[yt-dlp],%(progress._percent_str)s,%(progress._eta_str)s,..."
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
                            stdout: null, // stdall is now handling both filename and progress; stdout can be null
                            stderr: (item, error) => { // Ensure this callback also uses the item
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
            } // end while

            lock (_queueLock) {
                _isProcessingQueue = false;
            }
            System.Diagnostics.Debug.WriteLine("ProcessQueueAsync: Finished processing queue.");
        }

        private void LoadSettingsToUI() { // Renamed from LoadProxySettings
            if (Config.Default == null) {
                System.Diagnostics.Debug.WriteLine("Config.Default is null in LoadSettingsToUI. Attempting to load.");
                Config.Load();
                if (Config.Default == null) { // If still null, means Load didn't initialize it or file was empty
                    System.Diagnostics.Debug.WriteLine("Config.Default is still null after Load. Creating new Config instance for Default.");
                    Config.Default = new Config();
                }
            }
            // Proxy Settings
            ProxyEnableCheckBox.IsChecked = Config.Default.ProxyEnabled;
            ProxyUrlTextBox.Text = Config.Default.ProxyUrl ?? string.Empty;
            ProxyPortTextBox.Text = Config.Default.ProxyPort ?? string.Empty;
            ProxyUsernameTextBox.Text = Config.Default.ProxyUsername ?? string.Empty;
            ProxyPasswordTextBox.Text = Config.Default.ProxyPassword ?? string.Empty;
            System.Diagnostics.Debug.WriteLine("Proxy settings loaded into UI.");

            // Load Video Format Setting
            VideoFormatTextBox.Text = Config.Default.PreferredVideoFormat ?? "bv*+ba/b";
            System.Diagnostics.Debug.WriteLine($"Loaded PreferredVideoFormat: {VideoFormatTextBox.Text}");

            // Load Audio Options Settings
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

            // Load Download Folder Path
            DownloadPathTextBox.Text = Config.Default.DownloadFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "yt-dlp-gui");
            System.Diagnostics.Debug.WriteLine($"Loaded DownloadFolderPath: {DownloadPathTextBox.Text}");
        }

        public Main() {
            InitializeComponent();
            LoadSettingsToUI(); // Call LoadSettingsToUI

            DownloadQueue = new ObservableCollection<DownloadItem>();
            this.DataContext = this; // Set DataContext for data binding
            // DataContext = Data; // This was the previous DataContext. If still needed, consider a ViewModel that combines 'Data' and 'DownloadQueue'

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

            //Load Configs
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

            //Configuration Checking (./configs/*.*)
            InitConfiguration();

            //ScanDeps (YT-DLP, FFMPEG...)
            ScanDepends();

            //if `Target` Not exist, default app location
            if (!Directory.Exists(Data.TargetPath)) {
                Data.TargetPath = App.AppPath;
            }
            //Default Temp Dir
            if (string.IsNullOrWhiteSpace(Data.PathTEMP) || !Directory.Exists(GetTempPath)) {
                Data.PathTEMP = "%YTDLPGUI_TARGET%";
            }

            //Monitor Clipboard
            InitClipboard();

            //run update check
            Task.Run(Inits);

            //Output Lang templet
            //Yaml.Save(App.Path(App.Folders.root, "lang.yaml"), new Lang());
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
                {"%YTDLPGUI_LOCALE%", App.AppPath}
            };
            foreach (KeyValuePair<string, string> pair in replacements) {
                string placeholder = pair.Key;
                string replacement = pair.Value;

                // Replace the placeholder with the replacement string
                path = path.Replace(placeholder, replacement);

                // Remove the part to the left of the replacement string
                int index = path.IndexOf(replacement);
                if (index >= 0) {
                    path = path.Substring(index);
                }

                // Remove duplicate directory separators
                path = path.Replace('/', '\\');
                while (path.Contains("\\\\")) {
                    path = path.Replace("\\\\", "\\");
                }
            }
            return Environment.ExpandEnvironmentVariables(path);
        }
        private string GetTempPath {
            get => GetEnvPath(Data.PathTEMP);
        }
        //Regex For Clipboard
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
            int delayTime = 1000; // milliseconds
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
            Data.GUIConfig.Load(App.Path(App.Folders.root, App.AppName + ".yaml"));
            Util.PropertyCopy(Data.GUIConfig, Data);
            //Loaded and enabled auto save config
            Data.AutoSaveConfig = true;
        }
        public void InitConfiguration() {
            Data.Configs.Clear();
            Data.Configs.Add(new Config() { name = App.Lang.Main.ConfigurationNone });
            var cp = App.Path(App.Folders.configs);
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
                var deps = Directory.EnumerateFiles(App.AppPath, "*.exe", SearchOption.AllDirectories).ToList();
                deps = deps.Where(x => Path.GetFileName(App.AppExe) != Path.GetFileName(x)).ToList();
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
                if (Regex.IsMatch(DLP.Path_DLP, isYoutubeDl)) DLP.Type = DLP.DLPType.youtube_dl;
                if (string.IsNullOrWhiteSpace(DLP.Path_Aria2)) {
                    Data.PathAria2 = DLP.Path_Aria2 = dep_aria2;
                }
                if (string.IsNullOrWhiteSpace(FFMPEG.Path_FFMPEG)) {
                    Data.PathFFMPEG = DLP.Path_FFMPEG = FFMPEG.Path_FFMPEG = dep_ffmpeg;
                }
            }
        }
        public async void Inits() {
            //check update
            var needcheck = false;
            var currentDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"); //"";

            if (!string.IsNullOrWhiteSpace(Data.LastVersion)) needcheck = true; //not yaml
            if (currentDate != Data.LastCheckUpdate) needcheck = true; //cross date

            if (needcheck) {
                var releaseData = await Web.GetLastTag();
                var last = releaseData.FirstOrDefault();
                if (last != null) {
                    Data.ReleaseData = releaseData;
                    Data.LastVersion = last.tag_name;
                    Data.LastCheckUpdate = currentDate;
                }
            }
            if (string.Compare(App.CurrentVersion, Data.LastVersion) < 0) {
                Data.NewVersion = true;
            }
        }
        private void Button_Analyze(object sender, RoutedEventArgs e) {
            Analyze_Start();
        }
        private void Analyze_Start() {
            Data.IsAnalyze = true;
            cc.SelectedIndex = -1;
            cv.SelectedIndex = -1;
            ca.SelectedIndex = -1;
            cs.SelectedIndex = -1;
            Data.Thumbnail = null;
            Data.Video = new();
            Data.NeedCookie = Data.UseCookie == UseCookie.Always;

            Task.Run(() => {
                GetInfo();
                Data.IsAnalyze = false;

                if (Data.AutoDownloadAnalysed) {
                    //Download_Start();
                    if (Data.selectedVideo != null && Data.selectedAudio != null) {
                        Download_Start_Native();
                    }
                }
            });
        }
        private void GetInfo() {
            //Analyze
            var dlp = new DLP(Data.Url);
            if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
            dlp.Proxy(Data.ProxyUrl, Data.ProxyEnabled);
            dlp.GetInfo();
            if (!string.IsNullOrWhiteSpace(Data.selectedConfig.file)) {
                dlp.LoadConfig(Data.selectedConfig.file);
            }
            if (Data.UseOutput) dlp.Output("%(title)s.%(ext)s"); //if not used config, default template
            ClearStatus();
            dlp.Exec(null, std => {
                //取得JSON
                Data.Video = JsonConvert.DeserializeObject<Video>(std, new JsonSerializerSettings() {
                    NullValueHandling = NullValueHandling.Ignore
                });

                //Reading Chapters
                {
                    Data.Chapters.Clear();
                    if (Data.Video.chapters != null && Data.Video.chapters.Any()) {
                        Data.Chapters.Add(new Chapters() { title = App.Lang.Main.ChaptersAll, type = ChaptersType.None });
                        Data.Chapters.Add(new Chapters() { title = App.Lang.Main.ChaptersSplite, type = ChaptersType.Split });
                        Data.Chapters.AddRange(Data.Video.chapters);
                        Data.hasChapter = true;
                    } else {
                        Data.Chapters.Add(new Chapters() { title = App.Lang.Main.ChaptersNone, type = ChaptersType.None });
                        Data.hasChapter = false;
                    }
                    //Data.selectedChapter = Data.Chapters.First();
                }
                //读取 Formats 与 Thumbnails
                {
                    //Debug.WriteLine(JsonConvert.SerializeObject(Data.Video.chapters, Formatting.Indented));
                    Data.Formats.LoadFromVideo(Data.Video.formats);
                    Data.Thumbnails.Reset(Data.Video.thumbnails);
                    Data.RequestedFormats.LoadFromVideo(Data.Video.requested_formats);
                }
                //读取 Subtitles
                {
                    var subs = Data.Video.subtitles.Select(x => {
                        var s = x.Value.FirstOrDefault(y => y.ext == "vtt");
                        if (s == null) return null;
                        s.key = x.Key;
                        return s;
                    }).Where(x => x != null).ToList();
                    Data.Subtitles.Clear();
                    if (subs.Any()) {
                        Data.Subtitles.Add(new Subs() { name = App.Lang.Main.SubtitleIgnore });
                        Data.hasSubtitle = true;
                    } else {
                        Data.Subtitles.Add(new Subs() { name = App.Lang.Main.SubtitleNone });
                        Data.hasSubtitle = false;
                    }
                    Data.Subtitles.AddRange(subs);
                }
                var BestUrl = Data.Thumbnails.LastOrDefault()?.url;
                if (BestUrl != null && Web.Head(BestUrl)) {
                    Data.Thumbnail = BestUrl;
                } else {
                    Data.Thumbnail = Data.Video.thumbnail;
                }

                Data.SelectFormatBest(); //Make ComboBox Selected Item
                var full = string.Empty;
                if (Path.IsPathRooted(Data.Video._filename)) {
                    full = Path.GetFullPath(Data.Video._filename);
                } else {
                    full = Path.Combine(Data.TargetPath, Data.Video._filename);
                }
                //Data.TargetName = GetValidFileName(Data.Video.title) + ".tmp"; //预设挡案名称
                Data.TargetName = full; //预设挡案名称

            });
            dlp.Err(DLP.DLPError.Sign, () => {
                if (Data.UseCookie == UseCookie.WhenNeeded) {
                    Data.NeedCookie = true;
                    GetInfo();
                } else if (Data.UseCookie == UseCookie.Ask) {
                    var mb = System.Windows.Forms.MessageBox.Show(
                        $"{App.Lang.Dialog.CookieRequired}\n",
                        $"{App.AppName}",
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
            //SaveStream(0);
            var dialog = new SaveFileDialog();
            dialog.Filter =
                $"{App.Lang.Files.mkv}|*.mkv|" +
                $"{App.Lang.Files.mp4}|*.mp4|" +
                $"{App.Lang.Files.webm}|*.webm|" +
                $"{App.Lang.Files.mov}|*.mov|" +
                $"{App.Lang.Files.flv}|*.flv";
            dialog.DefaultExt = Data.selectedVideo.video_ext.ToLower();
            dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), dialog.DefaultExt);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = dialog.FileName;
                Download_Start_Native(DownloadType.Video, target);
            }
        }
        private void Button_SaveAudio(object sender, RoutedEventArgs e) {
            //SaveStream(1);
            var dialog = new SaveFileDialog();
            dialog.Filter =
                $"{App.Lang.Files.opus}|*.opus|" +
                $"{App.Lang.Files.aac}|*.aac|" +
                $"{App.Lang.Files.m4a}|*.m4a|" +
                $"{App.Lang.Files.mp3}|*.mp3|" +
                $"{App.Lang.Files.vorbis}|*.vorbis|" +
                $"{App.Lang.Files.alac}|*.alac|" +
                $"{App.Lang.Files.flac}|*.flac|" +
                $"{App.Lang.Files.wav}|*.wav";
            dialog.DefaultExt = Data.selectedAudio.acodec.ToLower();
            dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), dialog.DefaultExt);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = dialog.FileName;
                Download_Start_Native(DownloadType.Audio, target);
            }
        }
        private void Button_ExplorerTarget(object sender, RoutedEventArgs e) {
            Util.Explorer(Data.TargetFile);
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
            string url = Data.Url; // Assuming Data.Url holds the text from the main URL input
            if (string.IsNullOrWhiteSpace(url)) {
                MessageBox.Show("Please enter a valid URL.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning); // TODO: Localize
                return;
            }

            // Ensure DownloadFolderPath exists
            if (Config.Default == null) Config.Load(); // Make sure config is loaded
            string downloadDirectory = Config.Default?.DownloadFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "yt-dlp-gui");
            try {
                Directory.CreateDirectory(downloadDirectory);
            } catch (Exception ex) {
                MessageBox.Show($"Error creating download directory '{downloadDirectory}': {ex.Message}", "Directory Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Localize
                return;
            }

            var newItem = new DownloadItem(url) {
                // Populate from global config active at the time of adding
                SelectedVideoFormat = Config.Default?.PreferredVideoFormat ?? "bv*+ba/b",
                DownloadAudioOnly = Config.Default?.DownloadAudioOnly ?? false,
                SelectedAudioFormat = Config.Default?.PreferredAudioFormat ?? "mp3",
                FileName = "Fetching title...",
                OutputPath = downloadDirectory
            };

            DownloadQueue.Add(newItem);
            Data.Url = string.Empty; // Clear the URL box after adding to queue

            System.Diagnostics.Debug.WriteLine($"Added to queue: {newItem.Url}, Status: {newItem.Status}");

            if (!_isProcessingQueue) {
                Task.Run(() => ProcessQueueAsync());
            }
        }
        public enum DownloadType { Normal, Video, Audio, Thumbnail, Subtitle } // This might be moved or become part of DownloadItem
        private async void Download_Start_Native(DownloadType type = DownloadType.Normal, string target = "") { // This method will likely be refactored to process items from DownloadQueue
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
                //如果檔案已存在
                if (File.Exists(Data.TargetFile) && type == DownloadType.Normal) {
                    var mb = System.Windows.Forms.MessageBox.Show(
                        $"{App.Lang.Dialog.FileExist}\n",
                        $"{App.AppName}",
                        MessageBoxButtons.YesNo);
                    overwrite = mb == System.Windows.Forms.DialogResult.Yes;
                    if (!overwrite) return; //不要复写
                }
                Data.IsDownload = true;

                //進度更新為0
                ClearStatus();
                _ = Task.Run(() => {
                    var dlp = new DLP(Data.Url);
                    List<Task> tasks = new();
                    tasks.Add(Task.Run(() => {
                        //var dlp = new DLP(Data.Url);
                        RunningDLP.Add(dlp);
                        dlp.IsLive = Data.Video.is_live;
                        var vid = type switch {
                            DownloadType.Video => Data.selectedVideo.format_id,
                            DownloadType.Audio => Data.selectedAudio.format_id,
                            _ => $"{Data.selectedVideo.format_id}+{Data.selectedAudio.format_id}"
                        };
                        dlp
                        .Temp(GetTempPath)
                        .LoadConfig(Data.selectedConfig.file)
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
                                .Subtitle(Data.selectedSub.key, Data.TargetFile, Data.EmbedSubtitles)
                                .DownloadVideo(vid, Data.selectedVideo.video_ext, target);
                                break;
                            case DownloadType.Audio:
                                dlp
                                .EmbedChapters(Data.EmbedChapters)
                                .Thumbnail(Data.SaveThumbnail, Data.TargetFile, Data.EmbedThumbnail)
                                .DownloadAudio(vid, target);
                                break;
                            case DownloadType.Subtitle:
                                dlp.DownloadSubtitle(Data.selectedSub.key, target);
                                break;
                            default:
                                dlp
                                .EmbedChapters(Data.EmbedChapters)
                                .Thumbnail(Data.SaveThumbnail, Data.TargetFile, Data.EmbedThumbnail)
                                .Subtitle(Data.selectedSub.key, Data.TargetFile, Data.EmbedSubtitles)
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
                    //WaitAll Downloads, Merger Video and Audio
                    Data.CanCancel = true;
                    Task.WaitAll(tasks.ToArray());
                    if (!Data.IsAbouted) {
                        Data.DNStatus_Infos["Status"] = App.Lang.Status.Done;

                        //post-process
                        Dictionary<string, string> files = new Dictionary<string, string>();
                        foreach (string donepath in dlp.Files) {
                            if (File.Exists(donepath)) {
                                if (donepath.isVideo()) files["video"] = donepath;
                                if (donepath.isImage()) files["thumb"] = donepath;
                                if (Data.ModifiedType == ModifiedType.Upload) {
                                    if (DateTimeOffset.TryParseExact(Data.Video.upload_date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset tryDate)) {
                                        File.SetLastWriteTimeUtc(donepath, tryDate.DateTime);
                                    }
                                }
                            }
                        }

                        //Send notification when download completed
                        try {
                            if (Data.UseNotifications) {
                                Util.NotifySound(Data.PathNotify);
                                var toast = new ToastContentBuilder()
                                    .AddText(Data.Video.title)
                                    .AddText(App.Lang.Dialog.DownloadCompleted)
                                    .AddAudio(new ToastAudio() {
                                        Silent = true,
                                        Loop = false,
                                        Src = new Uri("ms-winsoundevent:Notification.Default")
                                    });
                                if (files.ContainsKey("video")) {
                                    toast.AddButton(
                                        new ToastButton()
                                        .SetContent(App.Lang.Dialog.OpenFolder)
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
                                    .SetContent(App.Lang.Dialog.Close)
                                    .AddArgument("action", "none")
                                    .SetBackgroundActivation()
                                );
                                toast.Show();
                            }
                        } catch (Exception ex) { }
                    }
                    //Clear downloading status
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
                dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile);
                dialog.FileName = Path.GetFileName(Data.TargetFile);
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    Data.RemuxVideo = true;
                    Data.TargetPath = Path.GetDirectoryName(dialog.FileName);
                    Data.TargetName = Path.GetFileName(dialog.FileName);
                    Data.RemuxVideo = false;
                    /*
                    if ((new string[] { ".mp4", ".webm", ".3gp", ".mkv" }).Any(x => Path.GetExtension(dialog.FileName).ToLower() == x)) {
                        Data.TargetName = Path.GetFileName(dialog.FileName);
                    } else {
                        Data.TargetName = Path.GetFileName(dialog.FileName) + ".tmp";
                    }
                    */
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
            dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile);
            var OrigExt = Path.GetExtension(Data.Thumbnail);
            var OrigFileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), OrigExt);
            dialog.DefaultExt = ".jpg";
            dialog.Filter = $"{App.Lang.Files.image}|*.jpg;*.webp";
            dialog.FileName = Path.ChangeExtension(OrigFileName, ".jpg");
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                DownloadThumbnail(dialog.FileName);
            }
        }
        private void DownloadThumbnail(string toFile) {
            var origExt = Path.GetExtension(Data.Thumbnail);
            var origin = Path.ChangeExtension(Data.TargetFile, origExt);
            //var target = Path.ChangeExtension(Data.TargetFile, ".jpg");
            var target = toFile;
            var progress = new Progress<double>(percentage => {
                Debug.Write($"\rDownloading... {percentage:0.00}%");
            });
            Web.Download(Data.Thumbnail, origin, progress, Data.ProxyEnabled ? Data.ProxyUrl : null).Wait();
            //convert to target ext
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
            dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile);
            //dialog.Filter = "SubRip | *.srt";
            //dialog.DefaultExt = Data.selectedSub.key + ".srt";
            dialog.DefaultExt = ".srt";
            dialog.Filter =
                $"{App.Lang.Files.srt}|*.srt|" +
                $"{App.Lang.Files.ass}|*.ass|" +
                $"{App.Lang.Files.vtt}|*.vtt|" +
                $"{App.Lang.Files.lrc}|*.lrc|" +
                $"{App.Lang.Files.ttml}|*.ttml|" +
                $"{App.Lang.Files.srv3}|*.srv3|" +
                $"{App.Lang.Files.srv2}|*.srv2|" +
                $"{App.Lang.Files.srv1}|*.srv1|" +
                $"{App.Lang.Files.json3}|*.json3";
            //dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), Data.selectedSub.key + ".srt");
            dialog.FileName = Path.ChangeExtension(Data.TargetFile, null);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = dialog.FileName;
                Debug.WriteLine(dialog.FileName, "DIALOG");
                //var target = Path.ChangeExtension(dialog.FileName, ".srt");
                //FFMPEG.DownloadUrl(Data.selectedSub.url, target);
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
            if (combo.SelectedIndex == -1) {
                Data.PathTEMP = combo.Text;
            } else {
                Data.PathTEMP = combo.SelectedValue.ToString();
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e) {
            var b = sender as ToggleButton;
            if (b.IsChecked == true) {
                var menu = new List<MenuDataItem>() {
                    (App.Lang.Main.TemporaryTarget, () => { Data.PathTEMP = "%YTDLPGUI_TARGET%"; }),
                    (App.Lang.Main.TemporaryLocale, () => { Data.PathTEMP = "%YTDLPGUI_LOCALE%"; }),
                    (App.Lang.Main.TemporarySystem, () => { Data.PathTEMP = "%TEMP%"; }),
                    ("-"),
                    (App.Lang.Main.TemporaryBrowse, () => {
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
            if (b.IsChecked == true) {
                var menu = new List<MenuDataItem>() {
                    (App.Lang.Main.SoundSystem, () => { Data.PathNotify = ""; }),
                    ("-"),
                    (App.Lang.Main.SoundBrowse, () => {
                        var dialog = new OpenFileDialog();
                        var dirname = Path.GetDirectoryName(Data.PathNotify);
                        Debug.WriteLine(dirname);
                        if (Directory.Exists(dirname)) {
                            dialog.InitialDirectory = dirname;
                            if (File.Exists(Data.PathNotify)) {
                                dialog.FileName = Path.GetFileName(Data.PathNotify);
                            }
                        } else {
                            dialog.InitialDirectory = App.AppPath;
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
            Util.NotifySound(Data.PathNotify);
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
            ThemeManager.ToggleTheme();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e) { // Renamed from SaveProxySettingsButton_Click
            if (Config.Default == null) {
                 System.Diagnostics.Debug.WriteLine("Config.Default is null in SaveSettingsButton_Click. Attempting to load.");
                 Config.Load();
                 if (Config.Default == null) { // Still null after load, create new
                    System.Diagnostics.Debug.WriteLine("Config.Default is still null after Load. Creating new Config instance for Default.");
                    Config.Default = new Config();
                 }
            }
            // Proxy Settings
            Config.Default.ProxyEnabled = ProxyEnableCheckBox.IsChecked ?? false;
            Config.Default.ProxyUrl = ProxyUrlTextBox.Text;
            Config.Default.ProxyPort = ProxyPortTextBox.Text;
            Config.Default.ProxyUsername = ProxyUsernameTextBox.Text;
            Config.Default.ProxyPassword = ProxyPasswordTextBox.Text;

            // Save Video Format Setting
            Config.Default.PreferredVideoFormat = VideoFormatTextBox.Text;
            System.Diagnostics.Debug.WriteLine($"Saved PreferredVideoFormat: {Config.Default.PreferredVideoFormat}");

            // Save Audio Options Settings
            Config.Default.DownloadAudioOnly = AudioOnlyCheckBox.IsChecked ?? false;
            if (AudioFormatComboBox.SelectedItem is ComboBoxItem selectedItem) {
                Config.Default.PreferredAudioFormat = selectedItem.Content?.ToString() ?? "mp3";
            } else if (!string.IsNullOrEmpty(AudioFormatComboBox.Text)) {
                 Config.Default.PreferredAudioFormat = AudioFormatComboBox.Text; // Handles if user types a custom format
            } else {
                Config.Default.PreferredAudioFormat = "mp3"; // Default fallback if somehow empty
            }
            System.Diagnostics.Debug.WriteLine($"Saved DownloadAudioOnly: {Config.Default.DownloadAudioOnly}, PreferredAudioFormat: {Config.Default.PreferredAudioFormat}");

            // Save Download Folder Path
            Config.Default.DownloadFolderPath = DownloadPathTextBox.Text;
            System.Diagnostics.Debug.WriteLine($"Saved DownloadFolderPath: {Config.Default.DownloadFolderPath}");

            Config.Default.Save(App.Path(App.Folders.root, App.AppName + ".yaml"));

            MessageBox.Show("Settings saved.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information); // TODO: Localize
            System.Diagnostics.Debug.WriteLine("Settings saved from UI.");
        }
    }
    public class LanguageConverter :IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            // 檢查輸入值是否為字串
            if (!(value is string key))
                return value;

            // 利用反射機制查詢 Lang 物件是否包含指定的 key 屬性
            var Lang = App.Lang.Status;
            var propertyInfo = Lang.GetType().GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            // 如果 Lang 物件不包含指定的 key 屬性，則返回空字串
            if (propertyInfo == null)
                return key;

            // 如果 Lang 物件包含指定的 key 屬性，則返回相應的值
            return propertyInfo.GetValue(Lang)?.ToString() ?? key;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            // ConvertBack 未實作，因為此轉換器僅用於單向綁定
            throw new NotImplementedException();
        }
    }
}

