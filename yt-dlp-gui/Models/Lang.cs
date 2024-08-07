using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Forms;
using YamlDotNet.Serialization;

namespace yt_dlp_gui.Models {
    public class Lang :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        [Description("Name")]
        public string AppName { get; set; } = "yt-dlp-gui";

        [Description("Window - Main")]
        public LangMain Main { get; set; } = new();

        [Description("Window - About")]
        public LangAbout About { get; set; } = new();

        [Description("Window - Release")]
        public LangReleases Releases { get; set; } = new();

        [Description("Dialog, Popups")]
        public LangDialog Dialog { get; set; } = new();

        [Description("File Types")]
        public LangFiles Files { get; set; } = new();

        [Description("Status Area Infomation")]
        public LangStatus Status { get; set; } = new();
    }
    public class LangStatus :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string Downloader { get; set; } = "Downloader";
        public string Total { get; set; } = "Total";
        public string Speed { get; set; } = "Speed";
        public string Elapsed { get; set; } = "Elapsed";
        public string Connections { get; set; } = "Connections";
        public string Frame { get; set; } = "Frame";
        public string FPS { get; set; } = "FPS";
        public string Time { get; set; } = "Time";
        public string Status { get; set; } = "Status";
        public string Native { get; set; } = "Native";
        public string Downloading { get; set; } = "Downloading";
        public string Downloaded { get; set; } = "Downloaded";
        public string Done { get; set; } = "Done";
    }
    public class LangFiles :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        //General
        public string image { get; set; } = "Image File";
        //Video
        public string mkv { get; set; } = "Matroska Multimedia Container";
        public string mp4 { get; set; } = "MPEG-4 Part 14";
        public string webm { get; set; } = "WebM Container Format";
        public string mov { get; set; } = "QuickTime Movie";
        public string flv { get; set; } = "Flash Video";
        //Audio
        public string opus { get; set; } = "Opus Interactive Audio Codec";
        public string aac { get; set; } = "Advanced Audio Coding";
        public string m4a { get; set; } = "MPEG-4 Audio Layer";
        public string mp3 { get; set; } = "MPEG-1 Audio Layer III";
        public string vorbis { get; set; } = "Vorbis Audio Codec";
        public string alac { get; set; } = "Apple Lossless Audio Codec";
        public string flac { get; set; } = "Free Lossless Audio Codec";
        public string wav { get; set; } = "Waveform Audio File Format";
        //Subtitle
        public string srt { get; set; } = "SubRip Subtitle Format";
        public string ass { get; set; } = "Advanced SubStation Alpha";
        public string vtt { get; set; } = "WebVTT (Web Video Text Tracks)";
        public string lrc { get; set; } = "Lyric File Format";
        public string ttml { get; set; } = "Timed Text Markup Language";
        public string srv3 { get; set; } = "SRV3 Format";
        public string srv2 { get; set; } = "SRV2 Format";
        public string srv1 { get; set; } = "SRV1 Format";
        public string json3 { get; set; } = "JSON3 Format";
    }
    public class LangDialog:INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string CookieRequired { get; set; } = "Cookies are required, Use it?";
        public string FileExist { get; set; } = "File Already exist. Overwrite it?";
        public string DownloadCompleted { get; set; } = "Video Download Completed!";
        public string OpenFolder { get; set; } = "Open Folder";
        public string Close { get; set; } = "Close";
    }
    public class LangMain :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        [Description("Top Menu")]
        [YamlMember(Order = 1000)] public string About { get; set; } = "About...";

        [Description("Tabs Label")]
        [YamlMember(Order = 1001)] public string Main { get; set; } = "Main";
        [YamlMember(Order = 1002)] public string Advance { get; set; } = "Advance";
        [YamlMember(Order = 1003)] public string Options { get; set; } = "Options";

        [Description("Main Tab")]
        [YamlMember(Order = 1011)] public string Url { get; set; } = "Url";
        [YamlMember(Order = 1012)] public string Title { get; set; } = "Title";
        [YamlMember(Order = 1013)] public string Desc { get; set; } = "Description";
        [YamlMember(Order = 1014)] public string Live { get; set; } = "LIVE";
        [YamlMember(Order = 1015)] public string DownloadThumb { get; set; } = "Download Thumbnail";

        [Description("Main Tab - Buttons")]
        [YamlMember(Order = 1021)] public string Analyze { get; set; } = "Analyze";
        [YamlMember(Order = 1022)] public string Browse { get; set; } = "Browse";
        [YamlMember(Order = 1023)] public string Download { get; set; } = "Download";
        [YamlMember(Order = 1024)] public string Record { get; set; } = "Record";
        [YamlMember(Order = 1025)] public string Cancel { get; set; } = "Cancel";
        [YamlMember(Order = 1026)] public string Stop { get; set; } = "Stop";

        [Description("Main Tab - Thumbnail")]
        [YamlMember(Order = 1031)] public string SaveAs { get; set; } = "Save As...";
        [YamlMember(Order = 1032)] public string Thumbnail { get; set; } = "Thumbnail";

        [Description("Main Tab - Chapters")]
        [YamlMember(Order = 1041)] public string Chapters { get; set; } = "Chapters";
        [YamlMember(Order = 1042)] public string ChaptersNone { get; set; } = "[None]";
        [YamlMember(Order = 1043)] public string ChaptersAll { get; set; } = "[All]";
        [YamlMember(Order = 1044)] public string ChaptersSplite { get; set; } = "[Split by Chapters]";

        [Description("Main Tab - Video")]
        [YamlMember(Order = 1051)] public string Video { get; set; } = "Video";
        [YamlMember(Order = 1052)] public string VideoRes { get; set; } = "Resolution";
        [YamlMember(Order = 1053)] public string VideoDynamicRange { get; set; } = "DR";
        [YamlMember(Order = 1054)] public string VideoFPS { get; set; } = "FPS";
        [YamlMember(Order = 1055)] public string VideoExt { get; set; } = "Ext.";
        [YamlMember(Order = 1056)] public string VideoCodec { get; set; } = "Codec";
        [YamlMember(Order = 1057)] public string VideoSize { get; set; } = "FileSize";

        [Description("Main Tab - Audio")]
        [YamlMember(Order = 1061)] public string Audio { get; set; } = "Audio";
        [YamlMember(Order = 1062)] public string AudioSampleRate { get; set; } = "ASR";
        [YamlMember(Order = 1063)] public string AudioExt { get; set; } = "Ext.";
        [YamlMember(Order = 1064)] public string AudioCodec { get; set; } = "Codec";
        [YamlMember(Order = 1065)] public string AudioSize { get; set; } = "FileSize";

        [Description("Main Tab - Subtitle")]
        [YamlMember(Order = 1071)] public string Subtitle { get; set; } = "Subtitle";
        [YamlMember(Order = 1072)] public string SubtitleIgnore { get; set; } = "[Ignore]";
        [YamlMember(Order = 1073)] public string SubtitleNone { get; set; } = "[None]";

        [Description("Advance Tab - Configuration")]
        [YamlMember(Order = 1081)] public string Configuration { get; set; } = "Configuration";
        [YamlMember(Order = 1082)] public string ConfigurationNone { get; set; } = "[None]";

        [Description("Advance Tab - Proxy")]
        [YamlMember(Order = 1091)] public string Proxy { get; set; } = "Proxy";
        [YamlMember(Order = 1092)] public string ProxyEnabled { get; set; } = "Enabled";
        [YamlMember(Order = 1093)] public string ProxyHelper { get; set; } = "socks5://user:pass@127.0.0.1:1080/";

        [Description("Advance Tab - Cookie")]
        [YamlMember(Order = 1101)] public string Cookie { get; set; } = "Cookie";
        [YamlMember(Order = 1102)] public string CookieWhenNeeded { get; set; } = "When Needed";
        [YamlMember(Order = 1103)] public string CookieNever { get; set; } = "Never";
        [YamlMember(Order = 1104)] public string CookieAlways { get; set; } = "Always";
        [YamlMember(Order = 1105)] public string CookieAsk { get; set; } = "Ask";
        [YamlMember(Order = 1106)] public string CookieUse { get; set; } = "Use";

        [Description("Advance Tab - Aria2")]
        [YamlMember(Order = 1111)] public string Aria2 { get; set; } = "Aria2";
        [YamlMember(Order = 1112)] public string Aria2Enabled { get; set; } = "Enabled";

        [Description("Advance Tab - Embeds")]
        [YamlMember(Order = 1121)] public string Embeds { get; set; } = "Embeds";
        [YamlMember(Order = 1122)] public string EmbedsThumbnail { get; set; } = "Thumbnail";
        [YamlMember(Order = 1123)] public string EmbedsChapters { get; set; } = "Chapters";
        [YamlMember(Order = 1124)] public string EmbedsSubtitles { get; set; } = "Subtitles";

        [Description("Advance Tab - Time Range")]
        [YamlMember(Order = 1131)] public string TimeRange { get; set; } = "Time Range";
        [YamlMember(Order = 1132)] public string TimeRangeHits { get; set; } = "--download-sections";
        [YamlMember(Order = 1133)] public string TimeRangeHelper { get; set; } = "*10:15-15:00";

        [Description("Advance Tab - Limit Rate")]
        [YamlMember(Order = 1141)] public string LimitRate { get; set; } = "Limit Rate";
        [YamlMember(Order = 1142)] public string LimitRateHelper { get; set; } = "50K or 4.2M";

        [Description("Advance Tab - Mdified")]
        [YamlMember(Order = 1151)] public string Modified { get; set; } = "Modified";
        [YamlMember(Order = 1152)] public string ModifiedModified { get; set; } = "Modified Date";
        [YamlMember(Order = 1153)] public string ModifiedCreated { get; set; } = "Created Date";
        [YamlMember(Order = 1153)] public string ModifiedUpload { get; set; } = "Upload Date";

        [Description("Options Tab - Notifications")]
        [YamlMember(Order = 1161)] public string Notifications { get; set; } = "Notifications";
        [YamlMember(Order = 1162)] public string NotificationsEnabled { get; set; } = "Enabled";
        [YamlMember(Order = 1163)] public string NotificationsSound { get; set; } = "Notifications Sound";
        [YamlMember(Order = 1164)] public string SoundDefault { get; set; } = "Default";
        [YamlMember(Order = 1165)] public string SoundSystem { get; set; } = "System";
        [YamlMember(Order = 1166)] public string SoundBrowse { get; set; } = "Browse...";

        [Description("Options Tab - Always On Top")]
        [YamlMember(Order = 1171)] public string AlwaysOnTop { get; set; } = "Always On Top";
        [YamlMember(Order = 1172)] public string AlwaysOnTopEnabled { get; set; } = "Enabled";

        [Description("Options Tab - Remember Window State")]
        [YamlMember(Order = 1181)] public string RememberWindowState { get; set; } = "Remember Window State";
        [YamlMember(Order = 1182)] public string RememberWindowPosition { get; set; } = "Position";
        [YamlMember(Order = 1183)] public string RememberWindowSize { get; set; } = "Size";
        [YamlMember(Order = 1184)] public string Scale { get; set; } = "Scale";

        [Description("Options Tab - Automatically download")]
        [YamlMember(Order = 1191)] public string AutoDownload { get; set; } = "Automatically download";
        [YamlMember(Order = 1192)] public string AutoDownloadAnalysed { get; set; } = "After analysed";

        [Description("Options Tab - Temporary folder")]
        [YamlMember(Order = 1201)] public string TemporaryFolder { get; set; } = "Temporary folder";
        [YamlMember(Order = 1202)] public string TemporaryTarget { get; set; } = "Target";
        [YamlMember(Order = 1203)] public string TemporaryLocale { get; set; } = "Locale";
        [YamlMember(Order = 1204)] public string TemporarySystem { get; set; } = "System";
        [YamlMember(Order = 1205)] public string TemporaryBrowse { get; set; } = "Browse...";
        [YamlMember(Order = 1207)] public string NameFormat { get; set; } = "Name format";

        //public string Formats { get; set; } = "Formats"; //Deprecated
        //public string EmbedSubs { get; set; } = "Embed Subtitles";
        //public string EmbedSubsEnabled { get; set; } = "Enabled";

    }
    public class LangAbout :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string About { get; set; } = "About";
        public string Website { get; set; } = "Website";
        public string Authors { get; set; } = "Authors";

        [Description("Extended information can be freely modified and there is no limit to the number of lines.\nExtends :\n  label1: content\n  label2: url")]
        public Dictionary<string, string> Extends { get; set; } = new();
    }
    public class LangReleases :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string Releases { get; set; } = "Releases";
        public string Loading { get; set; } = "Loading...";
        public string NoUpdated { get; set; } = "There is currently no updated version.";
    }
}
