using Libs;
using Libs.Yaml;
using Markdig;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using yt_dlp_gui.Models;
using yt_dlp_gui.Wrappers;

namespace yt_dlp_gui.Views {
    public partial class Main : Window {
        private readonly ViewData Data = new();
        private List<DLP> RunningDLP = new();
        public Main() {
            InitializeComponent();
            DataContext = Data;
            //Load Configs
            Data.Config.Load(App.Path(App.Folders.root, App.AppName + ".yaml"));
            Util.PropertyCopy(Data.Config, Data);

            Data.AutoSaveConfig = true;
            //Check Path
            if (!Directory.Exists(Data.TargetPath)) {
                Data.TargetPath = App.AppPath;
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
            dlp.Exec(std => {
                //取得JSON
                Data.Video = JsonConvert.DeserializeObject<Video>(std);

                //读取 Formats 与 Thumbnails
                {
                    Data.Formats.LoadFromVideo(Data.Video);
                    Data.Thumbnails.Reset(Data.Video.thumbnails);
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
                Data.TargetName = GetValidFileName(Data.Video.title); //预设挡案名称
            });
            dlp.Err(DLP.DLPError.Sign, () => {
                if (Data.UseCookie == UseCookie.WhenNeeded) {
                    Data.NeedCookie = true;
                    GetInfo();
                }
            });
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
                if (File.Exists(Data.TargetFile)) {
                    var mb = System.Windows.Forms.MessageBox.Show(
                        "File Already exist. Overwrite it?\nwe",
                        "yt-dlp-gui",
                        MessageBoxButtons.YesNo);
                    overwrite = mb == System.Windows.Forms.DialogResult.Yes;
                    if (!overwrite) return; //不要复写
                }
                Data.VideoPersent = Data.AudioPersent = 0;
                Data.VideoETA = Data.AudioETA = "0:00";
                Data.IsDownload = true;
                var r = new Regex(@"(?<=\[download]).*?(?<persent>[\w.]+)%(.*?(?<=ETA)(?<eta>.*))?");
                Task.Run(() => {
                    var temppath = App.AppPath;
                    var vid = Data.selectedVideo.format_id;
                    var vext = Data.selectedVideo.video_ext;
                    var vpath = Data.selectedVideo.type == FormatType.package
                    ? Data.TargetFile //package 直接存目标
                    : Path.Combine(temppath, $"{Data.Video.id}.{vid}.{vext}");
                    var aid = Data.selectedAudio.format_id;
                    var aext = Data.selectedAudio.audio_ext;
                    var apath = Path.Combine(temppath, $"{Data.Video.id}.{aid}.{aext}");
                    Data.CheckExtension();

                    List<Task> tasks = new();
                    //Download Video
                    tasks.Add(Task.Run(() => {
                        var dlp = new DLP(Data.Url);
                        if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
                        RunningDLP.Add(dlp);
                        dlp.DownloadFormat(vid, vpath);
                        dlp.Exec(stdout => {
                            var data = GetGroup(r, stdout);
                            Data.VideoPersent = decimal.Parse(data.GetValueOrDefault("persent", "0"));
                            Data.VideoETA = data.GetValueOrDefault("eta", "0:00");
                        });
                    }));
                    //Download Audio
                    if (Data.selectedVideo.type == FormatType.video) {
                        tasks.Add(Task.Run(() => {
                            var dlp = new DLP(Data.Url);
                            if (Data.NeedCookie) dlp.Cookie(Data.CookieType);
                            RunningDLP.Add(dlp);
                            dlp.DownloadFormat(aid, apath);
                            dlp.Exec(stdout => {
                                var data = GetGroup(r, stdout);
                                Data.AudioPersent = decimal.Parse(data.GetValueOrDefault("persent", "0"));
                                Data.AudioETA = data.GetValueOrDefault("eta", "0:00");
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
                    //WaitAll Downloads
                    Task.WaitAll(tasks.ToArray());
                    if (!Data.IsAbouted) {
                        if (Data.selectedVideo.type == FormatType.video) {
                            FFMPEG.Merger(overwrite, Data.TargetFile, vpath, apath);
                            if (File.Exists(vpath)) File.Delete(vpath);
                            if (File.Exists(apath)) File.Delete(apath);
                        }
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
    }
}

