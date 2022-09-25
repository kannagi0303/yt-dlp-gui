using Libs;
using Libs.Yaml;
using Newtonsoft.Json;
using Swordfish.NET.Collections.Auxiliary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Markup;
using yt_dlp_gui.Models;
using yt_dlp_gui.Wrappers;
using Microsoft.Toolkit.Uwp.Notifications;

namespace yt_dlp_gui.Views {
    public partial class Main :Window {
        private readonly ViewData Data = new();
        private List<DLP> RunningDLP = new();
        public Main() {
            InitializeComponent();

            DataContext = Data;
            //Load Configs
            InitGUIConfig();

            //檢查 Configuration
            InitConfiguration();

            //檢查儲存Path，假如設定沒有則預設為App所在Path
            if (!Directory.Exists(Data.TargetPath)) {
                Data.TargetPath = App.AppPath;
            }
            //檢查更新
            Task.Run(Inits);
        }
        public void InitGUIConfig() {
            Data.GUIConfig.Load(App.Path(App.Folders.root, App.AppName + ".yaml"));
            Util.PropertyCopy(Data.GUIConfig, Data);
            //讀取設定檔完成，開啟變更自動儲存
            Data.AutoSaveConfig = true;
        }
        public void InitConfiguration() {
            Data.Configs.Clear();
            Data.Configs.Add(new Config() { name = "[None]" });
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
        public async void Inits() {
            //檢查更新
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
            Data.IsAnalyze = true;
            cv.SelectedIndex = -1;
            ca.SelectedIndex = -1;
            cs.SelectedIndex = -1;
            Data.Thumbnail = null;
            Data.NeedCookie = Data.UseCookie == UseCookie.Always;

            Task.Run(() => {
                GetInfo();
                Data.IsAnalyze = false;
            });
        }
        private void GetInfo() {
            var dlp = new DLP(Data.Url);
            if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
            dlp.GetInfo();
            if (!string.IsNullOrWhiteSpace(Data.selectedConfig.file)) {
                dlp.LoadConfig(Data.selectedConfig.file);
            }
            if (Data.UseOutput) dlp.Output("%(title)s.%(ext)s"); //if not used config, default template
            ClearStatus();
            dlp.Exec(null, std => {
                //取得JSON
                Data.Video = JsonConvert.DeserializeObject<Video>(std);
                //Debug.WriteLine(std);

                //读取 Formats 与 Thumbnails
                {
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
                        Data.Subtitles.Add(new Subs() { name = "[Ignore]" });
                    } else {
                        Data.Subtitles.Add(new Subs() { name = "[None]" });
                    }
                    Data.Subtitles.AddRange(subs);
                }
                //读取 图片
                var BestUrl = Data.Thumbnails.LastOrDefault()?.url;
                if (Web.Head(BestUrl)) {
                    Data.Thumbnail = BestUrl;
                } else {
                    Data.Thumbnail = Data.Video.thumbnail;
                }

                Data.SelectFormatBest(); //选择
                var full = string.Empty;
                if (Path.IsPathRooted(Data.Video.filename)) {
                    full = Path.GetFullPath(Data.Video.filename);
                } else {
                    full = Path.Combine(Data.TargetPath, Data.Video.filename);
                }
                //Data.TargetName = GetValidFileName(Data.Video.title) + ".tmp"; //预设挡案名称
                Data.TargetName = full; //预设挡案名称
            });
            dlp.Err(DLP.DLPError.Sign, () => {
                if (Data.UseCookie == UseCookie.WhenNeeded) {
                    Data.NeedCookie = true;
                    GetInfo();
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
        private void GetStatus(string std, int chn = 0) {
            if (regDLP.IsMatch(std)) {
                if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = "Native";
                var d = std.Split(',');
                var s = (chn == 0)?Data.DNStatus_Video : Data.DNStatus_Audio;
                if (decimal.TryParse(d[4], out decimal d_total)) {
                    s.Total = d_total;
                    s.Persent = decimal.Parse(d[3]) / d_total * 100; ;
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
                if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = "aria2c";
                //Data.DNStatus_Downloader = "aria2c";
                var d = GetGroup(regAria, std);
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
                if (!Data.DNStatus_Infos.ContainsKey("Downloader")) Data.DNStatus_Infos["Downloader"] = "FFMPEG";
                var d = GetGroup(regFF, std);
                //Data.DNStatus_Downloader = "FFMPEG";
                Data.DNStatus_Infos["Downloaded"] = d.GetValueOrDefault("size", "");
                Data.DNStatus_Infos["Speed"] = d.GetValueOrDefault("bitrate", "");
                Data.DNStatus_Infos["Frame"] = d.GetValueOrDefault("frame", "");
                Data.DNStatus_Infos["FPS"] = d.GetValueOrDefault("fps", "");
                Data.DNStatus_Infos["Time"] = d.GetValueOrDefault("time", "");
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
                        if (Data.UseAria2) dlp.UseAria2();
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
        private void Button_Download(object sender, RoutedEventArgs e) {
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
                        "File Already exist. Overwrite it?\nwe",
                        "yt-dlp-gui",
                        MessageBoxButtons.YesNo);
                    overwrite = mb == System.Windows.Forms.DialogResult.Yes;
                    if (!overwrite) return; //不要复写
                }
                Data.IsDownload = true;
                //進度更新為0
                ClearStatus();
                Data.CheckExtension();

                //var r = new Regex(@"(?<=\[download|#\w{6}]?).*?(?<persent>[\w.]+)%(.*?(?<=ETA)(?<eta>.*))?");
                var tr = Data.TimeRange.Trim();
                //var tr = "*01:00-01:10";
                var isSingle = false;
                if (Data.selectedVideo.type == FormatType.package || !string.IsNullOrWhiteSpace(tr)) isSingle = true;

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
                        if (Data.UseAria2) dlp.UseAria2();

                        var vid = Data.selectedVideo.format_id;
                        if (!string.IsNullOrWhiteSpace(tr)) {
                            vid = vid += "+" + Data.selectedAudio.format_id;
                            dlp.DownloadSections(tr);
                        }
                        if (isSingle) {
                            dlp.DownloadFormat(vid, Data.TargetFile);
                        } else {
                            tmp_video_path = Path.Combine(App.AppPath, $"{Data.Video.id}.{vid}.{Data.selectedVideo.video_ext}");
                            dlp.DownloadFormat(vid, tmp_video_path);
                        }
                        dlp.Exec(std => {
                            //Debug.WriteLine(std);
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
                            if (Data.UseAria2) dlp.UseAria2();

                            var aid = Data.selectedAudio.format_id;
                            tmp_audio_path = Path.Combine(App.AppPath, $"{Data.Video.id}.{aid}.{Data.selectedAudio.audio_ext}");
                            dlp.DownloadFormat(aid, tmp_audio_path);

                            dlp.Exec(std => {
                                //Debug.WriteLine(std);
                                GetStatus(std, 1);
                            });
                        }));
                    }
                    //Download Subtitle
                    if (!string.IsNullOrWhiteSpace(Data.selectedSub?.url)) {
                        Data.SubtitlePersent = 0;
                        var subpath = Path.ChangeExtension(Data.TargetFile, Data.selectedSub.key + ".srt");
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
                    Task.WaitAll(tasks.ToArray());
                    if (!Data.IsAbouted) {
                        //Download Complete
                        if (!isSingle) {
                            FFMPEG.Merger(overwrite, Data.TargetFile, tmp_video_path, tmp_audio_path);
                            if (File.Exists(tmp_video_path)) File.Delete(tmp_video_path);
                            if (File.Exists(tmp_audio_path)) File.Delete(tmp_audio_path);
                        }
                        Data.DNStatus_Infos["Status"] = "Done";

                        //Send notification when download completed
                        if (Data.UseNotifications) {
                            new ToastContentBuilder()
                                .AddArgument("conversationId", 2333)
                                .AddText(Data.Video.title)
                                .AddText("Video Download Completed!")
                                .Show();
                        }
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
                    Data.TargetName = Path.GetFileName(dialog.FileName);
                }
            }
        }
        private Dictionary<string, string> GetGroup(Regex r, string input) {
            var m = r.Match(input);
            if (m.Success) {
                var groupData = r.GetGroupNames()
                    .Where(x => !string.IsNullOrWhiteSpace(m.Groups[x]?.Value))
                    .ToDictionary(x => x.ToLower(), x => m.Groups[x]);
                var group = groupData.ToDictionary(x => x.Key, x => x.Value.Value.Trim());
                return group;
            }
            return new Dictionary<string, string>();
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
    }
}

