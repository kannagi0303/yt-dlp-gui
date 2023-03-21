using Libs;
using Libs.Yaml;
using Swordfish.NET.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using YamlDotNet.Serialization;
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
                var defVideoFmt = FormatsVideo.FirstOrDefault();
                var defAudioFmt = FormatsAudio.FirstOrDefault();
                if (UseFormat) {
                    selectedVideo = defVideoFmt;
                    selectedAudio = defAudioFmt;
                } else {
                    selectedVideo = FormatsVideo.FirstOrDefault(x => RequestedFormats.Any(r => r.format_id == x.format_id), defVideoFmt);
                    selectedAudio = FormatsAudio.FirstOrDefault(x => RequestedFormats.Any(r => r.format_id == x.format_id), defAudioFmt);
                }
                selectedSub = Subtitles.FirstOrDefault();
            }
            public void CheckExtension() {
                if (RemuxVideo) return;
                if (!string.IsNullOrWhiteSpace(TargetName)) {
                    if (selectedVideo != null && selectedAudio != null) {
                        if (selectedVideo.type == FormatType.package) {
                            TargetName = Path.ChangeExtension(TargetName, selectedVideo.video_ext);
                        } else if (selectedVideo.video_ext == "webm" && selectedAudio.audio_ext == "webm") {
                            TargetName = Path.ChangeExtension(TargetName, "webm");
                        } else if (selectedVideo.video_ext == "mp4" && selectedAudio.audio_ext == "m4a") {
                            TargetName = Path.ChangeExtension(TargetName, "mp4");
                        } else {
                            TargetName = Path.ChangeExtension(TargetName, "mkv");
                        }
                    }
                }
            }
            public Lang Lang { get; set; } = new();
            public Video? Video { get; set; } = new();
            public ConcurrentObservableCollection<Config> Configs { get; set; } = new();
            public IEnumerable<Config> ConfigsView => Configs.CollectionView;
            public Config selectedConfig { get; set; } = new();
            public bool RemuxVideo { get; set; } = false;
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
            public bool hasChapter { get; set; } = false;
            public bool hasSubtitle { get; set; } = false;
            public Chapters? selectedChapter { get; set; } = null;
            public Format selectedVideo { get; set; } = new();
            public Format selectedAudio { get; set; } = new();
            public Subs selectedSub { get; set; } = new();
            public bool IsAnalyze { get; set; } = false;
            public bool IsDownload { get; set; } = false;
            public bool IsAbouted { get; set; } = false;
            public bool IsMonitor { get; set; } = false;
            public bool AlwaysOnTop { get; set; } = false;
            public bool AutoDownloadAnalysed { get; set; } = false;
            public bool RememberWindowStatePosition { get; set; } = false;
            public bool RememberWindowStateSize { get; set; } = false;
            public double Top { get; set; } = 0;
            public double Left { get; set; } = 0;
            public double Width { get; set; } = 600;
            public double Height { get; set; } = 380;
            public int Scale { get; set; } = 100;
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
            public bool EmbedThumbnail { get; set; } = false;
            public bool EmbedChapters { get; set; } = false;
            public bool EmbedSubtitles { get; set; } = false;
            public ModifiedType ModifiedType { get; set; } = ModifiedType.Modified;
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
            public string PathTEMP { get; set; } = string.Empty;
            public string PathNotify { get; set; } = string.Empty;
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
                Enable.SelectChapters = true;
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
                    Enable.SelectChapters = false;
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
            [Description("Paths")]
            [YamlMember(Order = 1001)] public string TargetPath { get; set; } = string.Empty;
            [YamlMember(Order = 1002)] public string PathYTDLP { get; set; } = string.Empty;
            [YamlMember(Order = 1003)] public string PathAria2 { get; set; } = string.Empty;
            [YamlMember(Order = 1004)] public string PathFFMPEG { get; set; } = string.Empty;
            [YamlMember(Order = 1005)] public string PathTEMP { get; set; } = string.Empty;
            [YamlMember(Order = 1006)] public string PathNotify { get; set; } = string.Empty;

            [Description("Window")]
            [YamlMember(Order = 1101)] public bool AlwaysOnTop { get; set; } = false;
            [YamlMember(Order = 1102)] public bool RememberWindowStatePosition { get; set; } = false;
            [YamlMember(Order = 1103)] public bool RememberWindowStateSize { get; set; } = false;
            [YamlMember(Order = 1104)] public double Top { get; set; } = 0;
            [YamlMember(Order = 1105)] public double Left { get; set; } = 0;
            [YamlMember(Order = 1106)] public double Width { get; set; } = 600;
            [YamlMember(Order = 1107)] public double Height { get; set; } = 380;
            [YamlMember(Order = 1108)] public int Scale { get; set; } = 100;

            [Description("Network")]
            [YamlMember(Order = 1201)] public bool ProxyEnabled { get; set; } = false;
            [YamlMember(Order = 1202)] public string ProxyUrl { get; set; } = string.Empty;
            [YamlMember(Order = 1203)] public UseCookie UseCookie { get; set; } = UseCookie.WhenNeeded;
            [YamlMember(Order = 1204)] public CookieType CookieType { get; set; } = CookieType.Chrome;

            [Description("Advance")]
            [YamlMember(Order = 1301)] public string ConfigurationFile { get; set; } = string.Empty;
            [YamlMember(Order = 1302)] public bool UseAria2 { get; set; } = false;
            [YamlMember(Order = 1303)] public bool EmbedThumbnail { get; set; } = false;
            [YamlMember(Order = 1304)] public bool EmbedChapters { get; set; } = false;
            [YamlMember(Order = 1305)] public bool EmbedSubtitles { get; set; } = false;
            [YamlMember(Order = 1306)] public string LimitRate { get; set; } = string.Empty;
            [YamlMember(Order = 1307)] public ModifiedType ModifiedType { get; set; } = ModifiedType.Modified;

            [Description("Options")]
            [YamlMember(Order = 1401)] public bool IsMonitor { get; set; } = false;
            [YamlMember(Order = 1402)] public bool SaveThumbnail { get; set; } = true;
            [YamlMember(Order = 1403)] public bool UseNotifications { get; set; } = true;
            [YamlMember(Order = 1404)] public bool AutoDownloadAnalysed { get; set; } = false;
            
            [Description("Last Checking Update Date")]
            [YamlMember(Order = 9001)] public string LastVersion { get; set; } = string.Empty;
            [YamlMember(Order = 9002)] public string LastCheckUpdate { get; set; } = string.Empty;
            //[Description("Embed Subtitles")] public bool EmbedSub { get; set; } = false;
        }
        public class DownloadStatus {
            public decimal Persent { get; set; } = 0;
            public decimal Downloaded { get; set; } = 0;
            public decimal Total { get; set; } = 0;
            public decimal Speed { get; set; } = 0;
            public decimal Elapsed { get; set; } = 0;
        }
        public class StatusRepoter {
            public int type = 0;
            private static Regex regPart = new Regex(@"\[download\] Destination:.*\.f(?<fid>\d+(?:-\w+)?)\.\w+");
            private static Regex regDLP = new Regex(@"^\[yt-dlp]");
            private static Regex regAria = new Regex(@"(?<=\[#\w{6}).*?(?<downloaded>[\w]+).*?\/(?<total>[\w]+).*?(?<persent>[\w.]+)%.*?CN:(?<cn>\d+).*DL:(?<speed>\w+)(.*?ETA:(?<eta>\w+))?");
            private static Regex regFF = new Regex(@"frame=.*?(?<frame>\d+).*?fps=.*?(?<fps>[\d.]+).*?size=.*?(?<size>\w+).*?time=(?<time>\S+).*?bitrate=(?<bitrate>\S+)");
            private static Regex regYTDL = new Regex(@"^\[download\].*?(?<persent>[\d\.]+).*?(?<=of).*?(?<total>\S+).*?(?<=at).*?(?<speed>\S+).*?(?<=ETA).*?(?<eta>\S+)");

            private ViewData Data { get; set; }
            private DownloadStatus s { get; set; }
            public StatusRepoter(ViewData data) {
                Data = data;
                s = Data.DNStatus_Video;
            }
            public void GetStatus(string std) {
                if (regPart.IsMatch(std)) {
                    var r = Util.GetGroup(regPart, std);
                    if (r.GetValueOrDefault("fid", "0") == Data.selectedVideo.format_id) {
                        type = 1;
                        s = Data.DNStatus_Video;
                    }
                    if (r.GetValueOrDefault("fid", "0") == Data.selectedAudio.format_id) {
                        type = 2;
                        s = Data.DNStatus_Audio; ;
                    }
                }
                if (s != null) {
                    if (regDLP.IsMatch(std)) {
                        // yt-dlp
                        if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = App.Lang.Status.Native;
                        var d = std.Split(',');
                        if (decimal.TryParse(d[4], out decimal d_total)) {
                            s.Total = d_total;
                            s.Persent = decimal.Parse(d[3]) / d_total * 100; ;
                        } else {
                            if (decimal.TryParse(d[1].TrimEnd('%'), out decimal d_persent)) {
                                s.Persent = d_persent;
                            }
                        }
                        s.Downloaded = decimal.Parse(d[3]);
                        if (decimal.TryParse(d[5], out decimal d_speed)) s.Speed = d_speed;
                        if (decimal.TryParse(d[6], out decimal d_elapsed)) s.Elapsed = d_elapsed;

                        UpdatePersent(s.Persent);

                        if (Data.DNStatus_Infos.ContainsKey("Downloader") && Data.DNStatus_Infos["Downloader"] == App.Lang.Status.Native) {
                            Data.DNStatus_Infos["Downloaded"] = Util.GetAutoUnit((long)Data.DNStatus_Video.Downloaded + (long)Data.DNStatus_Audio.Downloaded);
                            Data.DNStatus_Infos["Total"] = Util.GetAutoUnit((long)Data.DNStatus_Video.Total + (long)Data.DNStatus_Audio.Total);
                            Data.DNStatus_Infos["Speed"] = Util.GetAutoUnit((long)Data.DNStatus_Video.Speed + (long)Data.DNStatus_Audio.Speed);
                            Data.DNStatus_Infos["Elapsed"] = Util.SecToStr(Data.DNStatus_Video.Elapsed + Data.DNStatus_Audio.Elapsed);
                            Data.DNStatus_Infos["Status"] = App.Lang.Status.Downloading;
                        }
                    } else if (regAria.IsMatch(std)) {
                        // aria2
                        Data.DNStatus_Infos["Downloader"] = "aria2c";
                        var d = Util.GetGroup(regAria, std);
                        if (decimal.TryParse(d["persent"], out decimal o_persent)) {
                            UpdatePersent(o_persent);
                        }
                        Data.DNStatus_Infos["Downloaded"] = d["downloaded"];
                        Data.DNStatus_Infos["Total"] = d["total"];
                        Data.DNStatus_Infos["Speed"] = d["speed"];
                        Data.DNStatus_Infos["Elapsed"] = d.GetValueOrDefault("eta", "0s");
                        Data.DNStatus_Infos["Connections"] = d["cn"];
                        Data.DNStatus_Infos["Status"] = App.Lang.Status.Downloading;
                    } else if (regFF.IsMatch(std)) {
                        // ffmpeg
                        Data.DNStatus_Infos["Downloader"] = "FFMPEG";
                        var d = Util.GetGroup(regFF, std);
                        Data.DNStatus_Infos["Downloaded"] = d.GetValueOrDefault("size", "");
                        Data.DNStatus_Infos["Speed"] = d.GetValueOrDefault("bitrate", "");
                        Data.DNStatus_Infos["Frame"] = d.GetValueOrDefault("frame", "");
                        Data.DNStatus_Infos["FPS"] = d.GetValueOrDefault("fps", "");
                        Data.DNStatus_Infos["Time"] = d.GetValueOrDefault("time", "");
                        Data.DNStatus_Infos["Status"] = "Downloading";
                    } else if (regYTDL.IsMatch(std)) {
                        // youtube-dl
                        if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = "youtube-dl";
                        var d = Util.GetGroup(regYTDL, std);
                        if (decimal.TryParse(d["persent"], out decimal o_persent)) {
                            UpdatePersent(o_persent);
                        }
                        Data.DNStatus_Infos["Total"] = d.GetValueOrDefault("total", "");
                        Data.DNStatus_Infos["Speed"] = d.GetValueOrDefault("speed", "");
                        Data.DNStatus_Infos["Elapsed"] = d.GetValueOrDefault("eta", "");
                        Data.DNStatus_Infos["Status"] = "Downloading";
                    }
                }
            }
            private void UpdatePersent(decimal persent) {
                switch (type) {
                    case 0:
                        Data.VideoPersent = Data.AudioPersent = persent;
                        break;
                    case 1:
                        Data.VideoPersent = persent;
                        break;
                    case 2:
                        Data.AudioPersent = persent;
                        break;
                }
            }
        }
    }
}
