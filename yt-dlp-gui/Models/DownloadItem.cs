using System.ComponentModel;
using System.Runtime.CompilerServices; // Required for CallerMemberName
using System.Collections.Generic; // Required for EqualityComparer

namespace yt_dlp_gui.Models {
    public enum DownloadStatus {
        Queued,
        Downloading,
        Processing, // For things like remuxing or post-processing
        Completed,
        Failed,
        Paused,
        Cancelled
    }

    public class DownloadItem : INotifyPropertyChanged {
        private string _url = string.Empty;
        private DownloadStatus _status;
        private double _progress;
        private string _outputPath = string.Empty; // Full path where the file will be saved
        private string _fileName = string.Empty;   // Display name, can be set after getting info
        private string _selectedVideoFormat = string.Empty;
        private bool _downloadAudioOnly;
        private string _selectedAudioFormat = string.Empty;
        private string? _errorMessage;

        public string Url {
            get => _url;
            set => SetField(ref _url, value);
        }

        public DownloadStatus Status {
            get => _status;
            set => SetField(ref _status, value);
        }

        public double Progress { // Percentage 0-100
            get => _progress;
            set => SetField(ref _progress, value);
        }

        public string OutputPath {
            get => _outputPath;
            set => SetField(ref _outputPath, value);
        }

        public string FileName { // Could be determined by yt-dlp's -o template or title
            get => _fileName;
            set => SetField(ref _fileName, value);
        }

        public string SelectedVideoFormat {
            get => _selectedVideoFormat;
            set => SetField(ref _selectedVideoFormat, value);
        }

        public bool DownloadAudioOnly {
            get => _downloadAudioOnly;
            set => SetField(ref _downloadAudioOnly, value);
        }

        public string SelectedAudioFormat {
            get => _selectedAudioFormat;
            set => SetField(ref _selectedAudioFormat, value);
        }

        public string? ErrorMessage {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        public DownloadItem(string url) {
            Url = url;
            Status = DownloadStatus.Queued;
            Progress = 0;
            // Initialize other properties based on global config or leave for later
            // For example, copy global settings at the time of adding to queue:
            // if (Config.Default != null) {
            //     SelectedVideoFormat = Config.Default.PreferredVideoFormat;
            //     DownloadAudioOnly = Config.Default.DownloadAudioOnly;
            //     SelectedAudioFormat = Config.Default.PreferredAudioFormat; // This should be PreferredAudioFormat
            // }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
