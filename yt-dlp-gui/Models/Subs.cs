using System.ComponentModel;

namespace yt_dlp_gui.Models {
    public class Subs : INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string key { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string ext { get; set; } = string.Empty;
    }
}
