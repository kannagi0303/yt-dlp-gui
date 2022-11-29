using Libs;
using Libs.Yaml;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Swordfish.NET.Collections.Auxiliary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WK.Libraries.SharpClipboardNS;
using yt_dlp_gui.Models;
using yt_dlp_gui.Wrappers;

namespace yt_dlp_gui.Views {
    public partial class Main :Window {
        private readonly ViewData Data = new();
        private List<DLP> RunningDLP = new();
        public Main() {
            InitializeComponent();
            DataContext = Data;

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
            }

            //Configuration Checking
            InitConfiguration();

            //ScanDeps
            ScanDepends();

            //if `Target` Not exist, default app location
            if (!Directory.Exists(Data.TargetPath)) {
                Data.TargetPath = App.AppPath;
            }

            InitClipboard();

            //run update check
            Task.Run(Inits);
        }
        private Regex _frgPat = new Regex("<!--StartFragment-->(.*)<!--EndFragment-->", RegexOptions.Multiline | RegexOptions.Compiled);
        private Regex _matchUrls = new Regex(@"(https?|ftp|file)\://[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\(\)\*\-\._~%]*)*", RegexOptions.Compiled);
        public void InitClipboard() {
            Data.PropertyChanged += (s, e) => {
                switch (e.PropertyName) {
                    case nameof(Data.ClipboardText):
                        var content = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Html);
                        if (!string.IsNullOrWhiteSpace(content)) {
                            content = _frgPat.Match(content).Groups?[1].Value.Trim() ?? "";
                        } else {
                            content = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
                        }
                        var m = _matchUrls.Match(content);
                        if (m.Success) {
                            var capUrl = m.Value;
                            if (Util.UrlVaild(capUrl)) {
                                Data.Url = capUrl;
                                Analyze_Start();
                            }
                        }
                        //Debug.WriteLine($"Clipboard Change To: {ClipboardText}");
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
                    var text = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
                    Data.ClipboardText = text;
                }
            };
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
                ?Directory.EnumerateFiles(cp).OrderBy(x => x)
                :Enumerable.Empty<string>();
            fs.ForEach(x => {
                Data.Configs.Add(new Config() {
                    name = Path.GetFileNameWithoutExtension(x),
                    file = x
                });
            });
            Data.selectedConfig = Data.Configs.FirstOrDefault(x => x.file == Data.GUIConfig.ConfigurationFile, Data.Configs.First());
        }
        public void ScanDepends() {
            var deps = Directory.EnumerateFiles(App.AppPath, "*.exe", SearchOption.AllDirectories).ToList();
            deps = deps.Where(x => Path.GetFileName(App.AppExe) != Path.GetFileName(x)).ToList();
            var dep_youtubedl = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), @"^youtube-dl\.exe"), "");
            var dep_ytdlp = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), @"^(yt-dlp(_min|_x86|_x64)?|ytdl-patched.*?)\.exe"), "");
            var dep_ffmpeg = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), @"^ffmpeg"), "");
            var dep_aria2 = deps.FirstOrDefault(x => Regex.IsMatch(Path.GetFileName(x), @"^aria2"), "");
            if (!string.IsNullOrWhiteSpace(dep_ytdlp)) {
                DLP.Path_DLP = dep_ytdlp;
            } else if (!string.IsNullOrWhiteSpace(dep_youtubedl)) {
                DLP.Path_DLP = dep_youtubedl;
                DLP.Type = DLP.DLPType.youtube_dl;
            }
            DLP.Path_Aria2 = dep_aria2;
            FFMPEG.Path_FFMPEG = dep_ffmpeg;
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
            });
        }
        private void GetInfo() {
            //Analyze
            var dlp = new DLP(Data.Url);
            if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
            if (Data.ProxyEnabled && !string.IsNullOrWhiteSpace(Data.ProxyUrl)) dlp.Proxy(Data.ProxyUrl);
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
                    } else {
                        Data.Chapters.Add(new Chapters() { title = App.Lang.Main.ChaptersNone, type = ChaptersType.None });
                    }
                    //Data.selectedChapter = Data.Chapters.First();
                }
                //读取 Formats 与 Thumbnails
                {
                    Debug.WriteLine(JsonConvert.SerializeObject(Data.Video.chapters, Formatting.Indented));
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
                    } else {
                        Data.Subtitles.Add(new Subs() { name = App.Lang.Main.SubtitleNone });
                    }
                    Data.Subtitles.AddRange(subs);
                }
                var BestUrl = Data.Thumbnails.LastOrDefault()?.url;
                //var ThumbUrl = string.Empty;
                if (BestUrl != null && Web.Head(BestUrl)) {
                    Data.Thumbnail = BestUrl;
                    //ThumbUrl = BestUrl;
                } else {
                    Data.Thumbnail = Data.Video.thumbnail;
                    //ThumbUrl = Data.Video.thumbnail;
                }
                //Download Thumb to Temp Folder
                /*
                Directory.CreateDirectory(App.Path(App.Folders.temp));
                var ThumbPath = App.Path(App.Folders.temp, Path.ChangeExtension(Data.Video.id, ".jpg"));
                FFMPEG.DownloadUrl(ThumbUrl, ThumbPath);
                Data.Thumbnail = ThumbPath;
                */

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
                        "Cookies are required, Use it?\n",
                        "yt-dlp-gui",
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
        private Regex regDLP = new Regex(@"^\[yt-dlp]");
        private Regex regAria = new Regex(@"(?<=\[#\w{6}).*?(?<downloaded>[\w]+).*?\/(?<total>[\w]+).*?(?<persent>[\w.]+)%.*?CN:(?<cn>\d+).*DL:(?<speed>\w+)(.*?ETA:(?<eta>\w+))?");
        private Regex regFF = new Regex(@"frame=.*?(?<frame>\d+).*?fps=.*?(?<fps>[\d.]+).*?size=.*?(?<size>\w+).*?time=(?<time>\S+).*?bitrate=(?<bitrate>\S+)");
        private Regex regYTDL = new Regex(@"^\[download\].*?(?<persent>[\d\.]+).*?(?<=of).*?(?<total>\S+).*?(?<=at).*?(?<speed>\S+).*?(?<=ETA).*?(?<eta>\S+)");
        private void GetStatus(string std, int chn = 0) {
            //Debug.WriteLine(std, "STATUS");
            if (regDLP.IsMatch(std)) {
                // yt-dlp
                if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = "Native";
                var d = std.Split(',');
                var s = (chn == 0)?Data.DNStatus_Video : Data.DNStatus_Audio;
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
                if (chn == 0) {
                    Data.VideoPersent = s.Persent;
                } else {
                    Data.AudioPersent = s.Persent;
                }
                if (Data.DNStatus_Infos.ContainsKey("Downloader") && Data.DNStatus_Infos["Downloader"] != "Native") return;
                Data.DNStatus_Infos["Downloaded"] = Util.GetAutoUnit((long)Data.DNStatus_Video.Downloaded + (long)Data.DNStatus_Audio.Downloaded);
                Data.DNStatus_Infos["Total"] = Util.GetAutoUnit((long)Data.DNStatus_Video.Total + (long)Data.DNStatus_Audio.Total);
                Data.DNStatus_Infos["Speed"] = Util.GetAutoUnit((long)Data.DNStatus_Video.Speed + (long)Data.DNStatus_Audio.Speed);
                Data.DNStatus_Infos["Elapsed"] = Util.SecToStr(Data.DNStatus_Video.Elapsed + Data.DNStatus_Audio.Elapsed);
                Data.DNStatus_Infos["Status"] = "Downloading";
            } else if (regAria.IsMatch(std)) {
                // aria2
                if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = "aria2c";
                var d = Util.GetGroup(regAria, std);
                if (chn == 0) {
                    if (decimal.TryParse(d["persent"], out decimal o_persent)) Data.VideoPersent = o_persent;
                    Data.DNStatus_Infos["Downloaded"] = d["downloaded"];
                    Data.DNStatus_Infos["Total"] = d["total"];
                    Data.DNStatus_Infos["Speed"] = d["speed"];
                    Data.DNStatus_Infos["Elapsed"] = d.GetValueOrDefault("eta", "0s");
                    Data.DNStatus_Infos["Connections"] = d["cn"];
                } else {
                    if (decimal.TryParse(d["persent"], out decimal o_persent)) Data.AudioPersent = o_persent;
                }
                Data.DNStatus_Infos["Status"] = "Downloading";
            } else if (regFF.IsMatch(std)) {
                // ffmpeg
                if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = "FFMPEG";
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
                if (chn == 0) {
                    if (decimal.TryParse(d["persent"], out decimal o_persent)) Data.VideoPersent = o_persent;
                } else {
                    if (decimal.TryParse(d["persent"], out decimal o_persent)) Data.AudioPersent = o_persent;
                }
                Data.DNStatus_Infos["Total"] = d.GetValueOrDefault("total", "");
                Data.DNStatus_Infos["Speed"] = d.GetValueOrDefault("speed", "");
                Data.DNStatus_Infos["Elapsed"] = d.GetValueOrDefault("eta", "");
                Data.DNStatus_Infos["Status"] = "Downloading";
            }
        }
        private void Button_SaveVideo(object sender, RoutedEventArgs e) {
            SaveStream(0);

        }
        private void Button_SaveAudio(object sender, RoutedEventArgs e) {
            SaveStream(1);
        }
        private void SaveStream(int ch = 0) {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile);
            dialog.DefaultExt = ch == 0
                ? Data.selectedVideo.video_ext
                : Data.selectedAudio.audio_ext;
            dialog.Filter = "MediaFile | *." + dialog.DefaultExt;
            dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), "." + dialog.DefaultExt);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = Path.ChangeExtension(dialog.FileName, "." + dialog.DefaultExt);
                RunningDLP.Clear();
                Data.IsDownload = true;
                ClearStatus();
                Task.Run(() => {
                    //任務池
                    List<Task> tasks = new();
                    tasks.Add(Task.Run(() => {
                        var dlp = new DLP(Data.Url);
                        RunningDLP.Add(dlp);
                        if (!string.IsNullOrWhiteSpace(Data.selectedConfig.file)) dlp.LoadConfig(Data.selectedConfig.file);
                        if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
                        if (Data.ProxyEnabled && !string.IsNullOrWhiteSpace(Data.ProxyUrl)) dlp.Proxy(Data.ProxyUrl);
                        if (Data.UseAria2) dlp.UseAria2();
                        dlp.IsLive = Data.Video.is_live;

                        var vid = ch == 0
                        ?Data.selectedVideo.format_id
                        :Data.selectedAudio.format_id;
                        dlp.DownloadFormat(vid, target);
                        dlp.Exec(std => {
                            GetStatus(std, ch);
                        });
                    }));
                    //WaitAll Downloads, Merger Video and Audio
                    Task.WaitAll(tasks.ToArray());
                    if (!Data.IsAbouted) Data.DNStatus_Infos["Status"] = "Done";
                    Data.IsDownload = false;
                });
            }
        }
        private void Button_ExplorerTarget(object sender, RoutedEventArgs e) {
            //Debug.WriteLine(Data.TargetDisplay, "TargetDisplay");
            //Debug.WriteLine(Data.TargetFile, "TargetFile");
            //Debug.WriteLine(Data.TargetName, "TargetName");
            //Debug.WriteLine(Data.TargetPath, "TargetPath");
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
                if (File.Exists(Data.TargetFile)) {
                    var mb = System.Windows.Forms.MessageBox.Show(
                        "File Already exist. Overwrite it?\n",
                        "yt-dlp-gui",
                        MessageBoxButtons.YesNo);
                    overwrite = mb == System.Windows.Forms.DialogResult.Yes;
                    if (!overwrite) return; //不要复写
                }
                Data.IsDownload = true;
                //進度更新為0
                ClearStatus();
                Data.CheckExtension();

                var tr = Data.TimeRange.Trim();
                var isSingle = false;
                if (Data.selectedVideo.type == FormatType.package) isSingle = true;
                if (!string.IsNullOrWhiteSpace(tr)) isSingle = true;
                if (Data.selectedChapter != null && Data.selectedChapter.type == ChaptersType.Segment) isSingle = true;
                

                Task.Run(() => {
                    //任務池
                    List<Task> tasks = new();
                    var tmp_video_path = string.Empty;
                    var tmp_audio_path = string.Empty;
                    //Download Video (or Packaged)
                    tasks.Add(Task.Run(() => {
                        var dlp = new DLP(Data.Url);
                        RunningDLP.Add(dlp);
                        if (!string.IsNullOrWhiteSpace(Data.selectedConfig.file)) dlp.LoadConfig(Data.selectedConfig.file);
                        if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
                        if (Data.ProxyEnabled && !string.IsNullOrWhiteSpace(Data.ProxyUrl)) dlp.Proxy(Data.ProxyUrl);
                        if (Data.UseAria2) dlp.UseAria2();
                        dlp.IsLive = Data.Video.is_live;

                        var vid = Data.selectedVideo.format_id;
                        if (!string.IsNullOrWhiteSpace(tr)) {
                            vid = Data.selectedVideo.format_id + "+" + Data.selectedAudio.format_id;
                            dlp.DownloadSections(tr);
                        }
                        if (Data.selectedChapter != null && Data.selectedChapter.type == ChaptersType.Segment) {
                            vid = Data.selectedVideo.format_id + "+" + Data.selectedAudio.format_id;
                            dlp.DownloadSections(Data.selectedChapter.title);
                        }

                        tmp_video_path = Path.Combine(App.AppPath, $"{Data.Video.id}.{vid}.{Data.selectedVideo.video_ext}");
                        if (dlp.IsLive) {
                            tmp_video_path = Data.TargetFile;
                        }
                        dlp.DownloadFormat(vid, tmp_video_path);
                        Debug.WriteLine("Download Video");
                        dlp.Exec(std => {
                            Debug.WriteLine(std, "V");
                            GetStatus(std, 0);
                        });
                    }));
                    //Download Audio
                    if (!isSingle) {
                        tasks.Add(Task.Run(() => {
                            var dlp = new DLP(Data.Url);
                            RunningDLP.Add(dlp);
                            if (!string.IsNullOrWhiteSpace(Data.selectedConfig.file)) dlp.LoadConfig(Data.selectedConfig.file);
                            if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
                            if (Data.ProxyEnabled && !string.IsNullOrWhiteSpace(Data.ProxyUrl)) dlp.Proxy(Data.ProxyUrl);
                            if (Data.UseAria2) dlp.UseAria2();
                            dlp.IsLive = Data.Video.is_live;

                            var aid = Data.selectedAudio.format_id;
                            tmp_audio_path = Path.Combine(App.AppPath, $"{Data.Video.id}.{aid}.{Data.selectedAudio.audio_ext}");
                            dlp.DownloadFormat(aid, tmp_audio_path);
                            Debug.WriteLine("Download Audio");
                            dlp.Exec(std => {
                                //Debug.WriteLine(std, "A");
                                GetStatus(std, 1);
                            });
                        }));
                    }
                    //Download Subtitle
                    var subpath = string.Empty;
                    if (!string.IsNullOrWhiteSpace(Data.selectedSub?.url)) {
                        Data.SubtitlePersent = 0;
                        subpath = Path.ChangeExtension(Data.TargetFile, Data.selectedSub.key + ".srt");
                        FFMPEG.DownloadUrl(Data.selectedSub.url, subpath);
                        Data.SubtitlePersent = 100;
                    }
                    //Download Thumbnail
                    if (Data.SaveThumbnail) {
                        if (!string.IsNullOrWhiteSpace(Data.Thumbnail)) {
                            var thumbpath = Path.ChangeExtension(Data.TargetFile, ".jpg");
                            FFMPEG.DownloadUrl(Data.Thumbnail, thumbpath);
                        }
                    }

                    //WaitAll Downloads, Merger Video and Audio
                    Data.CanCancel = true;
                    Task.WaitAll(tasks.ToArray());
                    if (!Data.IsAbouted) {
                        //Download Complete
                        if (!isSingle) {
                            if (Data.EmbedSub && File.Exists(subpath)) {
                                //Subtitle
                                FFMPEG.Merger(overwrite, Data.TargetFile, tmp_video_path, tmp_audio_path, subpath);
                                File.Delete(subpath);
                            } else {
                                FFMPEG.Merger(overwrite, Data.TargetFile, tmp_video_path, tmp_audio_path);
                            }
                            if (File.Exists(tmp_video_path)) File.Delete(tmp_video_path);
                            if (File.Exists(tmp_audio_path)) File.Delete(tmp_audio_path);
                        } else {
                            if (Data.EmbedSub && File.Exists(subpath)) {
                                //Subtitle
                                FFMPEG.Merger(overwrite, Data.TargetFile, tmp_video_path, subpath);
                                File.Delete(subpath);
                            } else {
                                File.Move(tmp_video_path, Data.TargetFile, true);
                            }
                            if (File.Exists(tmp_video_path)) File.Delete(tmp_video_path);
                        }
                        //Splite By Chapters
                        if (Data.selectedChapter != null && Data.selectedChapter.type == ChaptersType.Split) {
                            var tar_info = new FileInfo(Data.TargetFile);
                            var tar_name = Path.GetFileNameWithoutExtension(tar_info.Name);
                            var tar_path = tar_info.Directory.FullName;
                            var tar_exts = tar_info.Extension;
                            var cidx = 0;
                            foreach (var c in Data.Video.chapters) {
                                cidx++;
                                var tar_seg_path = Path.Combine(tar_path, $"{tar_name} - {cidx}{tar_exts}");
                                Debug.WriteLine( tar_seg_path );
                                FFMPEG.Split(tar_seg_path, Data.TargetFile, c);
                            }
                            if (File.Exists(Data.TargetFile)) File.Delete(Data.TargetFile);
                        }

                        Data.DNStatus_Infos["Status"] = "Done";

                        //Send notification when download completed
                        try {
                            if (Data.UseNotifications) {
                                new ToastContentBuilder()
                                    .AddArgument("conversationId", 2333)
                                    .AddText(Data.Video.title)
                                    .AddText("Video Download Completed!")
                                    .Show();
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
                    Data.TargetPath = Path.GetDirectoryName(dialog.FileName);
                    if ((new string[] { ".mp4", ".webm", ".3gp" }).Any(x => Path.GetExtension(dialog.FileName).ToLower() == x)) {
                        Data.TargetName = Path.GetFileName(dialog.FileName);
                    } else {
                        Data.TargetName = Path.GetFileName(dialog.FileName) + ".tmp";
                    }
                }
            }
        }

        private static Regex RegexValues = new Regex(@"\${(.+?)}", RegexOptions.Compiled);
        private string GetValidFileName(string filename) {
            var regexSearch = new string(Path.GetInvalidFileNameChars());
            return Regex.Replace(filename, string.Format("[{0}]", Regex.Escape(regexSearch)), "_");
        }
        private void CommandBinding_SaveAs_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile);
            dialog.DefaultExt = ".jpg";
            dialog.Filter = "JPEG | *.jpg";
            dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), ".jpg");
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = Path.ChangeExtension(dialog.FileName, ".jpg");
                FFMPEG.DownloadUrl(Data.Thumbnail, target);
            }
        }

        private void CommandBinding_SaveAs_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e) {
            e.CanExecute = !string.IsNullOrWhiteSpace(Data.Thumbnail);
        }

        private void Button_Subtitle(object sender, RoutedEventArgs e) {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = Path.GetDirectoryName(Data.TargetFile);
            dialog.DefaultExt = ".srt";
            dialog.Filter = "SubRip | *.srt";
            dialog.FileName = Path.ChangeExtension(Path.GetFileName(Data.TargetFile), Data.selectedSub.key + ".srt");
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var target = Path.ChangeExtension(dialog.FileName, ".srt");
                FFMPEG.DownloadUrl(Data.selectedSub.url, target);
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
    }
}

