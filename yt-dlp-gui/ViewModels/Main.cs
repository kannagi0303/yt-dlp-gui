using Libs;
using Libs.Yaml;
using Swordfish.NET.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using yt_dlp_gui.Models;

namespace yt_dlp_gui.Views {
    public partial class Main : Window {
        public class ViewData : INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public ViewData() {
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
                PropertyChanged += SelfData_PropertyChanged;
            }
            private void SelfData_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
                switch (e.PropertyName) {
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

                if (AutoSaveConfig) Util.PropertyCopy(this, Config);
            }

            public void SelectFormatBest() {
                selectedVideo = FormatsVideo.FirstOrDefault();
                selectedAudio = FormatsAudio.FirstOrDefault();
                selectedSub = Subtitles.FirstOrDefault();
            }
            public void CheckExtension() {
                if (!string.IsNullOrWhiteSpace(TargetName)) {
                    if (selectedVideo != null && selectedAudio != null) {
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
            public Video? Video { get; set; } = new();
            public ConcurrentObservableCollection<Format> Formats { get; set; } = new();
            public IEnumerable<Format> FormatsView => Formats.CollectionView.OrderBy(x => x.width * x.height);
            public IEnumerable<Format> FormatsVideo => Formats.CollectionView.Where(x => x.type == FormatType.package || x.type == FormatType.video).OrderBy(x => x, ComparerVideo.Comparer);
            public IEnumerable<Format> FormatsAudio => Formats.CollectionView.Where(x => x.type == FormatType.package || x.type == FormatType.audio).OrderBy(x => x, ComparerAudio.Comparer);
            public ConcurrentObservableCollection<Thumb> Thumbnails { get; set; } = new();
            public IEnumerable<Thumb> ThumbnailsView => Thumbnails.CollectionView;
            public ConcurrentObservableCollection<Subs> Subtitles { get; set; } = new();
            public IEnumerable<Subs> SubtitlesView => Subtitles.CollectionView;
            public Format selectedVideo { get; set; } = new();
            public Format selectedAudio { get; set; } = new();
            public Subs selectedSub { get; set; } = new();
            public bool IsAnalyze { get; set; } = false;
            public bool IsDownload { get; set; } = false;
            public bool IsAbouted { get; set; } = false;
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
            public UseCookie UseCookie { get; set; } = UseCookie.WhenNeeded;
            public CookieType CookieType { get; set; } = CookieType.Chrome;
            public bool NeedCookie { get; set; } = false;
            public bool SaveThumbnail { get; set; } = true;
            public Enable Enable { get; set; } = new();
            public bool AutoSaveConfig { get; set; } = false;
            public string Html { get; set; } = string.Empty;
            public string LastVersion { get; set; } = string.Empty;
            public string LastCheckUpdate { get; set; } = string.Empty;
            public bool NewVersion { get; set; } = false;
            public List<GitRelease> ReleaseData { get; set; } = new();
            public Config Config { get; set; } = new();
            private void CheckEnable() {
                Enable.Url = true;
                Enable.Analyze = true;
                Enable.FormatVideo = true;
                Enable.FormatAudio = true;
                Enable.Download = true;
                Enable.Browser = true;
                Enable.SelectSubtitle = true;
                Enable.UseCookie = true;
                Enable.CookieType = true;
                Enable.SaveThumbnail = true;
                Enable.SaveSubtitle = true;

                if (string.IsNullOrWhiteSpace(Url)) Enable.Analyze = false;
                if (IsAnalyze) {
                    Enable.Url = false;
                    Enable.Analyze = false;
                    Enable.FormatVideo = false;
                    Enable.FormatAudio = false;
                    Enable.Download = false;
                    Enable.Browser = false;
                    Enable.SelectSubtitle = false;
                    Enable.UseCookie = false;
                    Enable.CookieType = false;
                    Enable.SaveThumbnail = false;
                    Enable.SaveSubtitle = false;
                }
                if (IsDownload) {
                    Enable.Url = false;
                    Enable.Analyze = false;
                    Enable.FormatVideo = false;
                    Enable.FormatAudio = false;
                    Enable.Browser = false;
                    Enable.SelectSubtitle = false;
                    Enable.UseCookie = false;
                    Enable.CookieType = false;
                    Enable.SaveThumbnail = false;
                    Enable.SaveSubtitle = false;
                }
                if (!FormatsVideo.Any()) Enable.FormatVideo = false;
                if (!FormatsAudio.Any()) Enable.FormatAudio = false;
                if (selectedVideo == null || selectedAudio == null) {
                    Enable.Download = false;
                } else {
                    if (string.IsNullOrWhiteSpace(selectedVideo.format_id)) {
                        Enable.Download = false;
                    }
                    if (string.IsNullOrWhiteSpace(selectedAudio.format_id)) {
                        Enable.Download = false;
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
            }

        }
        public class Enable : INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public bool Url { get; set; } = true;
            public bool Analyze { get; set; } = true;
            public bool FormatVideo { get; set; } = true;
            public bool FormatAudio { get; set; } = true;
            public bool Download { get; set; } = true;
            public bool Browser { get; set; } = true;
            public bool SelectSubtitle { get; set; } = true;
            public bool UseCookie { get; set; } = true;
            public bool CookieType { get; set; } = true;
            public bool SaveThumbnail { get; set; } = true;
            public bool SaveSubtitle { get; set; } = true;
        }
        public class Config : IYamlConfig, INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public Config() {
                PropertyChanged += Config_PropertyChanged;
            }
            private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
                this.Save();
            }
            [Description("Last Browsed Folder")] public string TargetPath { get; set; } = string.Empty;
            [Description("Use Cookie From Browser")] public UseCookie UseCookie { get; set; } = UseCookie.WhenNeeded;
            public CookieType CookieType { get; set; } = CookieType.Chrome;
            [Description("With Thumbnail When Downlaod")]
            public bool SaveThumbnail { get; set; } = true;

            [Description("Last Checking Update Date")]
            public string LastVersion { get; set; } = string.Empty;
            public string LastCheckUpdate { get; set; } = string.Empty;
        }
    }
}
