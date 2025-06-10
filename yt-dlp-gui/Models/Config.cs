using System;
using System; // Added by previous overwrite, ensure it's kept
using System.ComponentModel;
using Libs.Yaml; // Assuming this is needed for Yaml.Open/Save
using yt_dlp_gui.App; // Assuming this is needed for App.Path, App.Folders, App.AppName
using System.IO; // Required for Path.Combine

namespace yt_dlp_gui.Models {
    public enum UseCookie {
        Never,
        WhenNeeded,
        Always,
        Ask
    }
    public enum CookieType {
        Chrome,
        Edge,
        Firefox,
        Opera,
        Vivaldi,
        Chromium,
        Chrome_Beta
    }
    public enum ModifiedType {
        Modified,
        Created,
        Upload
    }

    public class Config : INotifyPropertyChanged {
        public static Config Default { get; set; } = new Config(); // To allow static Load/Save behavior

        public string PathYTDLP { get; set; } = string.Empty;
        public string PathAria2 { get; set; } = string.Empty;
        public string PathFFMPEG { get; set; } = string.Empty;
        public string PathTEMP { get; set; } = string.Empty;
        public string PathNotify { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public string ConfigurationFile { get; set; } = string.Empty;
        public bool AutoDownloadAnalysed { get; set; } = false;
        public bool SaveThumbnail { get; set; } = true;
        public bool EmbedThumbnail { get; set; } = true;
        public bool EmbedChapters { get; set; } = true;
        public bool EmbedSubtitles { get; set; } = false;
        public UseCookie UseCookie { get; set; } = UseCookie.WhenNeeded;
        public CookieType CookieType { get; set; } = CookieType.Chrome;

        // Proxy Settings - Start
        public bool ProxyEnabled { get; set; } = false;
        public string ProxyUrl { get; set; } = string.Empty;
        public string ProxyPort { get; set; } = string.Empty;
        public string ProxyUsername { get; set; } = string.Empty;
        public string ProxyPassword { get; set; } = string.Empty;
        // Proxy Settings - End

        public string PreferredVideoFormat { get; set; } = "bv*+ba/b";
        public bool DownloadAudioOnly { get; set; } = false;
        public string PreferredAudioFormat { get; set; } = "mp3";
        public string DownloadFolderPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "yt-dlp-gui");

        public bool UseAria2 { get; set; } = true;
        public string TimeRange { get; set; } = string.Empty;
        public string LimitRate { get; set; } = string.Empty;
        public ModifiedType ModifiedType { get; set; } = ModifiedType.Modified;
        public bool UseNotifications { get; set; } = true;
        public bool AlwaysOnTop { get; set; } = true;
        public bool RememberWindowStatePosition { get; set; } = true;
        public bool RememberWindowStateSize { get; set; } = true;
        public int Scale { get; set; } = 100;
        public double Left { get; set; } = 0;
        public double Top { get; set; } = 0;
        public double Width { get; set; } = 0;
        public double Height { get; set; } = 0;

        // Properties from the existing minimal Config class
        public string name { get; set; } = string.Empty;
        public string file { get; set; } = string.Empty;

        [YamlIgnore]
        public bool AutoSaveConfig { get; set; } = false;

        public Config() {
            PropertyChanged += (s, e) => {
                if (AutoSaveConfig && Default == this) { // Only save if it's the Default instance
                    Save(App.Path(App.Folders.root, App.AppName + ".yaml"));
                }
            };
        }

        public static void Load() {
            var loadedConfig = Yaml.Open<Config>(App.Path(App.Folders.root, App.AppName + ".yaml"));
            if (loadedConfig != null) {
                Default = loadedConfig;
            } else {
                Default = new Config(); // Initialize with defaults if file doesn't exist or is empty
            }
            Default.AutoSaveConfig = true;
        }

        public void Save(string path) {
            Yaml.Save(path, this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        // Consider implementing OnPropertyChanged method for INotifyPropertyChanged
    }
}
