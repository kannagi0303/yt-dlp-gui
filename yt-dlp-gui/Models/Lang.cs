using System.Collections.Generic;
using System.ComponentModel;

namespace yt_dlp_gui.Models {
    public class Lang :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string AppName { get; set; } = "yt-dlp-gui";
        public LangMain Main { get; set; } = new();
        public LangAbout About { get; set; } = new();
        public LangReleases Releases { get; set; } = new();
    }
    public class LangMain :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        //Basic
        public string Url { get; set; } = "Url";
        public string Title { get; set; } = "Title";
        public string Desc { get; set; } = "Description";
        public string SaveAs { get; set; } = "Save As...";
        public string Thumbnail { get; set; } = "Thumbnail";
        //Menu
        public string About { get; set; } = "About...";
        //Tab Format
        public string Formats { get; set; } = "Formats";
        public string Chapters { get; set; } = "Chapters";
        public string ChaptersNone { get; set; } = "[None]";
        public string ChaptersAll { get; set; } = "[All]";
        public string ChaptersSplite { get; set; } = "[Split by Chapters]";
        public string Video { get; set; } = "Video";
        public string Audio { get; set; } = "Audio";
        public string Subtitle { get; set; } = "Subtitle";
        public string SubtitleIgnore { get; set; } = "[Ignore]";
        public string SubtitleNone { get; set; } = "[None]";
        public string VideoRes { get; set; } = "Resolution";
        public string VideoDynamicRange { get; set; } = "DR";
        public string VideoFPS { get; set; } = "FPS";
        public string VideoExt { get; set; } = "Ext.";
        public string VideoCodec { get; set; } = "Codec";
        public string VideoSize { get; set; } = "FileSize";
        public string AudioSampleRate { get; set; } = "ASR";
        public string AudioExt { get; set; } = "Ext.";
        public string AudioCodec { get; set; } = "Codec";
        public string AudioSize { get; set; } = "FileSize";
        //Tab Advance
        public string Advance { get; set; } = "Advance";
        public string EmbedSubs { get; set; } = "Embed Subtitles";
        public string EmbedSubsEnabled { get; set; } = "Enabled";
        public string TimeRange { get; set; } = "Time Range";
        public string TimeRangeHits { get; set; } = "--download-sections";
        public string TimeRangeHelper { get; set; } = "*10:15-15:00";
        public string LimitRate { get; set; } = "Limit Rate";
        public string LimitRateHelper { get; set; } = "50K or 4.2M";
        //Tab Options
        public string Options { get; set; } = "Options";
        public string Notifications { get; set; } = "Notifications";
        public string NotificationsEnabled { get; set; } = "Enabled";
        public string AlwaysOnTop { get; set; } = "Always On Top";
        public string AlwaysOnTopEnabled { get; set; } = "Enabled";
        public string RememberWindowState { get; set; } = "Remember Window State";
        public string RememberWindowPosition { get; set; } = "Position";
        public string RememberWindowSize { get; set; } = "Size";
        public string Proxy { get; set; } = "Proxy";
        public string ProxyEnabled { get; set; } = "Enabled";
        public string ProxyHelper { get; set; } = "socks5://user:pass@127.0.0.1:1080/";
        public string Cookie { get; set; } = "Cookie";
        public string CookieWhenNeeded { get; set; } = "When Needed";
        public string CookieNever { get; set; } = "Never";
        public string CookieAlways { get; set; } = "Always";
        public string CookieAsk { get; set; } = "Ask";
        public string CookieUse { get; set; } = "Use";
        public string Configuration { get; set; } = "Configuration";
        public string ConfigurationNone { get; set; } = "[None]";
        public string Aria2 { get; set; } = "Aria2";
        public string Aria2Enabled { get; set; } = "Enabled";
        public string AutoDownload { get; set; } = "Automatically download";
        public string AutoDownloadClipboard { get; set; } = "Upon clipboard detection";

        //Label
        public string Live { get; set; } = "LIVE";
        //checkbox
        public string DownloadThumb { get; set; } = "Download Thumbnail";
        //button
        public string Analyze { get; set; } = "Analyze";
        public string Browse { get; set; } = "Browse";
        public string Download { get; set; } = "Download";
        public string Record { get; set; } = "Record";
        public string Cancel { get; set; } = "Cancel";
        public string Stop { get; set; } = "Stop";
  
    }
    public class LangAbout :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string About { get; set; } = "About";
        public string Website { get; set; } = "Website";
        public string Authors { get; set; } = "Authors";
        public Dictionary<string, string> Extends { get; set; } = new();
    }
    public class LangReleases :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string Releases { get; set; } = "Releases";
        public string Loading { get; set; } = "Loading...";
        public string NoUpdated { get; set; } = "There is currently no updated version.";
    }
}
