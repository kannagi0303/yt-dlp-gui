using Libs;
using Libs.Yaml;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Swordfish.NET.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using yt_dlp_gui.Models;

namespace yt_dlp_gui.Views {
    public partial class Main :Window {
        public class ViewData :INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public ViewData() {
                Chapters.PropertyChanged += (s, e) => {
                    switch (e.PropertyName) {
                        case nameof(ConcurrentObservableCollection<Format>.CollectionView):
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChaptersView)));
                            break;
                    }
                };
                Formats.PropertyChanged += (s, e) => {
                    switch (e.PropertyName) {
                        case nameof(ConcurrentObservableCollection<Format>.CollectionView):
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormatsVideo)));
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormatsAudio)));
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormatsView)));
                            break;
                    }
                };
                Thumbnails.PropertyChanged += (s, e) => {
                    switch (e.PropertyName) {
                        case nameof(ConcurrentObservableCollection<Format>.CollectionView):
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThumbnailsView)));
                            break;
                    }
                };
                Subtitles.PropertyChanged += (s, e) => {
                    switch (e.PropertyName) {
                        case nameof(ConcurrentObservableCollection<Format>.CollectionView):
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubtitlesView)));
                            break;
                    }
                };
                DNStatus_Infos.PropertyChanged += (s, e) => {
                    switch (e.PropertyName) {
                        case nameof(ConcurrentObservableDictionary<string, string>.CollectionView):
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DNStatus_InfosView)));
                            break;
                    }
                };
                PropertyChanged += SelfData_PropertyChanged;
            }
            private Regex _isComment = new Regex(@"^\s*#");
            private Regex _hasOutput = new Regex(@"(-o|--output)\s+");
            private Regex _hasFormat = new Regex(@"(-f|--format)\s+");
            private void SelfData_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
                switch (e.PropertyName) {
                    case nameof(selectedConfig):
                        var f = selectedConfig.file;
                        Debug.WriteLine(f, "config file");
                        GUIConfig.ConfigurationFile = f;

                        UseFormat = true;
                        UseOutput = true;

                        if (File.Exists(f)) {
                            foreach (var line in File.ReadLines(f)) {
                                if (string.IsNullOrWhiteSpace(line) || _isComment.IsMatch(line)) continue;
                                if (_hasFormat.IsMatch(line)) UseFormat = false;
                                if (_hasOutput.IsMatch(line)) UseOutput = false;
                            }
                        }

                        break;
                    case nameof(selectedVideo):
                        //更改package连接
                        if (selectedVideo != null && selectedAudio != null) {
                            if (selectedVideo.type == FormatType.package) {
                                IsPackage = true;
                                selectedAudio = selectedVideo;
                            } else {
                                IsPackage = false;
                                if (selectedAudio.type == FormatType.package) {
                                    selectedAudio = FormatsAudio.FirstOrDefault(x => x.type != FormatType.package);
                                }
                            }
                        }
                        CheckExtension();
                        break;
                    case nameof(selectedAudio):
                        CheckExtension();
                        break;
                    case nameof(TargetPath):
                        if (TargetPath.Last() != Path.DirectorySeparatorChar) TargetPath += Path.DirectorySeparatorChar;
                        TargetFile = Path.Combine(TargetPath, TargetName);
                        break;
                    case nameof(TargetName):
                        CheckExtension();
                        TargetFile = Path.Combine(TargetPath, TargetName);
                        break;
                    case nameof(TargetFile):
                        TargetDisplay = Util.ReplaceSpecialPath(TargetFile);
                        break;
                    case nameof(ImageWidth):
                        ImageHeight = ImageWidth * 0.5625d;
                        break;
                }
                CheckEnable();

                if (AutoSaveConfig) Util.PropertyCopy(this, GUIConfig);
            }
            public void SelectFormatBest() {
                selectedChapter = Chapters.FirstOrDefault();
                if (UseFormat) {
                    selectedVideo = FormatsVideo.FirstOrDefault();
                    selectedAudio = FormatsAudio.FirstOrDefault();
                } else {
                    selectedVideo = FormatsVideo.FirstOrDefault(x => RequestedFormats.Any(r => r.format_id == x.format_id));
                    selectedAudio = FormatsAudio.FirstOrDefault(x => RequestedFormats.Any(r => r.format_id == x.format_id));
                }
                selectedSub = Subtitles.FirstOrDefault();
            }
            public void CheckExtension() {
                if (!string.IsNullOrWhiteSpace(TargetName)) {
                    if (selectedVideo != null && selectedAudio != null) {
                        if (Path.HasExtension(TargetName) && Path.GetExtension(TargetName).ToLower() == ".mkv") {
                            TargetName = Path.ChangeExtension(TargetName, "mkv");
                        } else {
                            if (selectedVideo.type == FormatType.package) {
                                TargetName = Path.ChangeExtension(TargetName, selectedVideo.video_ext);
                            } else if (selectedVideo.video_ext == "webm" && selectedAudio.audio_ext == "webm") {
                                TargetName = Path.ChangeExtension(TargetName, "webm");
                            } else if (selectedVideo.video_ext == "mp4" && selectedAudio.audio_ext == "m4a") {
                                TargetName = Path.ChangeExtension(TargetName, "mp4");
                            } else {
                                TargetName = Path.ChangeExtension(TargetName, "mp4");
                            }
                        }
                    }
                }
            }
            public Lang Lang { get; set; } = new();
            public Video? Video { get; set; } = new();
            public ConcurrentObservableCollection<Config> Configs { get; set; } = new();
            public IEnumerable<Config> ConfigsView => Configs.CollectionView;
            public Config selectedConfig { get; set; } = new();
            public bool UseFormat { get; set; } = true;
            public bool UseOutput { get; set; } = true;
            public ConcurrentObservableCollection<Chapters> Chapters { get; set; } = new();
            public IEnumerable<Chapters> ChaptersView => Chapters.CollectionView;
            public ConcurrentObservableCollection<Format> Formats { get; set; } = new();
            public IEnumerable<Format> FormatsView => Formats.CollectionView.OrderBy(x => x.width * x.height);
            public IEnumerable<Format> FormatsVideo => Formats.CollectionView.Where(x => x.type == FormatType.package || x.type == FormatType.video).OrderBy(x => x, ComparerVideo.Comparer);
            public IEnumerable<Format> FormatsAudio => Formats.CollectionView.Where(x => x.type == FormatType.package || x.type == FormatType.audio).OrderBy(x => x, ComparerAudio.Comparer);
            public ConcurrentObservableCollection<Format> RequestedFormats { get; set; } = new();
            public ConcurrentObservableCollection<Thumb> Thumbnails { get; set; } = new();
            public IEnumerable<Thumb> ThumbnailsView => Thumbnails.CollectionView;
            public ConcurrentObservableCollection<Subs> Subtitles { get; set; } = new();
            public IEnumerable<Subs> SubtitlesView => Subtitles.CollectionView;
            public Chapters? selectedChapter { get; set; } = null;
            public Format selectedVideo { get; set; } = new();
            public Format selectedAudio { get; set; } = new();
            public Subs selectedSub { get; set; } = new();
            public bool IsAnalyze { get; set; } = false;
            public bool IsDownload { get; set; } = false;
            public bool IsAbouted { get; set; } = false;
            public bool IsMonitor { get; set; } = false;
            public bool AlwaysOnTop { get; set; } = false;
            public bool AutoDownloadClipboard { get; set; } = false;
            public bool RememberWindowStatePosition { get; set; } = false;
            public bool RememberWindowStateSize { get; set; } = false;
            public double Top { get; set; } = 0;
            public double Left { get; set; } = 0;
            public double Width { get; set; } = 600;
            public double Height { get; set; } = 380;
            public bool ProxyEnabled { get; set; } = false;
            public string ProxyUrl { get; set; } = string.Empty;
            public bool CanCancel { get; set; } = false;
            public string Url { get; set; } = string.Empty;
            public string CommandLine { get; set; } = string.Empty;
            public bool IsPackage { get; set; } = false;
            public decimal VideoPersent { get; set; } = 0;
            public string VideoETA { get; set; } = "0:00";
            public decimal AudioPersent { get; set; } = 0;
            public string AudioETA { get; set; } = "0:00";
            public decimal SubtitlePersent { get; set; } = 0;
            public string TargetPath { get; set; } = string.Empty;
            public string TargetName { get; set; } = string.Empty;
            public string TargetFile { get; set; } = string.Empty;
            public string TargetDisplay { get; set; } = string.Empty;
            public string? Thumbnail { get; set; } = null;
            public double ImageWidth { get; set; } = 0; //Binding 16:9
            public double ImageHeight { get; set; } = 0;
            public string ExecText { get; set; } = string.Empty;
            public UseCookie UseCookie { get; set; } = UseCookie.WhenNeeded;
            public CookieType CookieType { get; set; } = CookieType.Chrome;
            public bool UseNotifications { get; set; } = true;
            public bool UseAria2 { get; set; } = true;
            public bool NeedCookie { get; set; } = false;
            public bool SaveThumbnail { get; set; } = true;
            public bool EmbedSub { get; set; } = false;
            public string TimeRange { get; set; } = string.Empty;
            public string LimitRate { get; set; } = string.Empty;
            public Enable Enable { get; set; } = new();
            public bool AutoSaveConfig { get; set; } = false;
            public string Html { get; set; } = string.Empty;
            public string LastVersion { get; set; } = string.Empty;
            public string LastCheckUpdate { get; set; } = string.Empty;
            public string PathYTDLP { get; set; } = string.Empty;
            public string PathAria2 { get; set; } = string.Empty;
            public string PathFFMPEG { get; set; } = string.Empty;
            public bool NewVersion { get; set; } = false;
            public List<GitRelease> ReleaseData { get; set; } = new();
            public GUIConfig GUIConfig { get; set; } = new();
            //status
            public DownloadStatus DNStatus_Video { get; set; } = new();
            public DownloadStatus DNStatus_Audio { get; set; } = new();
            //public Dictionary<string, string> DNStatus_Infos { get; set; } = new();
            public ConcurrentObservableDictionary<string, string> DNStatus_Infos { get; set; } = new();
            public IEnumerable<KeyValuePair<string, string>> DNStatus_InfosView
                => DNStatus_Infos.CollectionView;
            public string ClipboardText { get; set; } = string.Empty;
            //
            private void CheckEnable() {
                Enable.Url = true;
                Enable.Analyze = true;
                Enable.SelectChapters= true;
                Enable.FormatVideo = true;
                Enable.FormatAudio = true;
                Enable.Download = true;
                Enable.Browser = true;
                Enable.SelectSubtitle = true;
                Enable.UseCookie = true;
                Enable.CookieType = true;
                Enable.SaveThumbnail = true;
                Enable.SaveVideo = true;
                Enable.SaveAudio = true;
                Enable.SaveSubtitle = true;
                Enable.UseNotifications = true;
                Enable.UseAria2 = true;

                if (string.IsNullOrWhiteSpace(Url)) Enable.Analyze = false;
                if (IsAnalyze) {
                    Enable.Url = false;
                    Enable.Analyze = false;
                    Enable.SelectChapters= false;
                    Enable.FormatVideo = false;
                    Enable.FormatAudio = false;
                    Enable.Download = false;
                    Enable.Browser = false;
                    Enable.SelectSubtitle = false;
                    Enable.UseCookie = false;
                    Enable.CookieType = false;
                    Enable.SaveThumbnail = false;
                    Enable.SaveVideo = false;
                    Enable.SaveAudio = false;
                    Enable.SaveSubtitle = false;
                }
                if (IsDownload) {
                    Enable.Url = false;
                    Enable.Analyze = false;
                    Enable.SelectChapters = false;
                    Enable.FormatVideo = false;
                    Enable.FormatAudio = false;
                    Enable.Browser = false;
                    Enable.SelectSubtitle = false;
                    Enable.UseCookie = false;
                    Enable.CookieType = false;
                    Enable.SaveThumbnail = false;
                    Enable.SaveVideo = false;
                    Enable.SaveAudio = false;
                    Enable.SaveSubtitle = false;
                    Enable.UseNotifications = false;
                    Enable.UseAria2 = false;
                }
                if (Video.chapters == null) Enable.SelectChapters = false;
                if (!FormatsVideo.Any()) {
                    Enable.FormatVideo = false;
                    Enable.SaveVideo = false;
                }
                if (!FormatsAudio.Any()) {
                    Enable.FormatAudio = false;
                    Enable.SaveAudio = false;
                }
                if (selectedVideo == null || selectedAudio == null) {
                    Enable.Download = false;
                    Enable.SaveVideo = false;
                    Enable.SaveAudio = false;
                } else {
                    if (string.IsNullOrWhiteSpace(selectedVideo.format_id)) {
                        Enable.Download = false;
                        Enable.SaveVideo = false;
                    }
                    if (string.IsNullOrWhiteSpace(selectedAudio.format_id)) {
                        Enable.Download = false;
                        Enable.SaveAudio = false;
                    }
                    if (selectedVideo.type == FormatType.package) Enable.FormatAudio = false;
                }
                if (Subtitles.Count <= 1) {
                    Enable.SelectSubtitle = false;
                    Enable.SaveSubtitle = false;
                } else {
                    if (string.IsNullOrWhiteSpace(selectedSub?.url)) {
                        Enable.SaveSubtitle = false;
                    }
                }
                if (Video.is_live == true) {
                    //ExecText = IsDownload ? "Stop" : "Record";
                    ExecText = IsDownload ? App.Lang.Main.Stop : App.Lang.Main.Record;
                } else {
                    //ExecText = IsDownload ? "Cancel" : "Download";
                    ExecText = IsDownload ? App.Lang.Main.Cancel : App.Lang.Main.Download;
                }
            }
        }
        public class Enable :INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public bool Url { get; set; } = true;
            public bool Analyze { get; set; } = true;
            public bool SelectChapters { get; set; } = true;
            public bool FormatVideo { get; set; } = true;
            public bool FormatAudio { get; set; } = true;
            public bool Download { get; set; } = true;
            public bool Browser { get; set; } = true;
            public bool SelectSubtitle { get; set; } = true;
            public bool UseCookie { get; set; } = true;
            public bool CookieType { get; set; } = true;
            public bool SaveThumbnail { get; set; } = true;
            public bool SaveVideo { get; set; } = true;
            public bool SaveAudio { get; set; } = true;
            public bool SaveSubtitle { get; set; } = true;
            public bool UseNotifications { get; set; } = true;
            public bool UseAria2 { get; set; } = true;
        }
        public class GUIConfig :IYamlConfig, INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public GUIConfig() {
                PropertyChanged += Config_PropertyChanged;
            }
            private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
                this.Save();
            }
            [Description("Last Browsed Folder")] public string TargetPath { get; set; } = string.Empty;
            [Description("Window - Always on Top")]
            public bool AlwaysOnTop { get; set; } = false;
            [Description("Window - Remember State")]
            public bool RememberWindowStatePosition { get; set; } = false;
            public bool RememberWindowStateSize { get; set; } = false;
            public double Top { get; set; } = 0;
            public double Left { get; set; } = 0;
            public double Width { get; set; } = 600;
            public double Height { get; set; } = 380;
            [Description("Proxy")]
            public bool ProxyEnabled { get; set; } = false;
            public string ProxyUrl { get; set; } = string.Empty;
            [Description("Use Cookie From Browser")] public UseCookie UseCookie { get; set; } = UseCookie.WhenNeeded;
            public CookieType CookieType { get; set; } = CookieType.Chrome;
            [Description("With Thumbnail When Downlaod")] public bool SaveThumbnail { get; set; } = true;
            [Description("Embed Subtitles")] public bool EmbedSub { get; set; } = false;
            [Description("UseNotifications")] public bool UseNotifications { get; set; } = true;
            public bool IsMonitor { get; set; } = false;
            [Description("Configuration File")] public string ConfigurationFile { get; set; } = string.Empty;
            [Description("Aria2 Settings")] public bool UseAria2 { get; set; } = false;
            [Description("Automatically download Upon clipboard detection")] public bool AutoDownloadClipboard { get; set; } = false;
            [Description("Automatically download Upon clipboard detection")] public string LimitRate { get; set; } = string.Empty;
            [Description("Paths")] 
            public string PathYTDLP { get; set; } = string.Empty;
            public string PathAria2 { get; set; } = string.Empty;
            public string PathFFMPEG { get; set; } = string.Empty;
            [Description("Last Checking Update Date")] public string LastVersion { get; set; } = string.Empty;
            public string LastCheckUpdate { get; set; } = string.Empty;

        }
        public class DownloadStatus {
            public decimal Persent { get; set; } = 0;
            public decimal Downloaded { get; set; } = 0;
            public decimal Total { get; set; } = 0;
            public decimal Speed { get; set; } = 0;
            public decimal Elapsed { get; set; } = 0;
        }
    }
}
