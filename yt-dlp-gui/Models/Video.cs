using System.Collections.Generic;
using System.ComponentModel;

namespace yt_dlp_gui.Models {
    public class Video : INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string id { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public List<Format> formats { get; set; } = new();
        public List<Thumb> thumbnails { get; set; } = new();
        public Dictionary<string, List<Subs>> subtitles { get; set; } = new();
        public string thumbnail { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal duration { get; set; } = 0;
        public string duration_string { get; set; } = string.Empty;
        public List<Format> requested_formats { get; set; } = new();
        public string filename { get; set; } = string.Empty;
        public bool is_live { get; set; } = false;
    }
}
