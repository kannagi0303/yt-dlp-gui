using System.ComponentModel;

namespace yt_dlp_gui.Models {
    public class Thumb : INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string url { get; set; } = string.Empty;
        public decimal preference { get; set; } = 0;
    }
}
