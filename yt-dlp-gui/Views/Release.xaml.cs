using Libs;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace yt_dlp_gui.Views {
    /// <summary>
    /// Release.xaml 的互動邏輯
    /// </summary>
    public partial class Release : Window {
        private readonly ReleaseData Data = new();
        public Release() {
            InitializeComponent();

            DataContext = Data;
            Task.Run(Inits);
        }
        public async void Inits() {
            var releaseData = await Web.GetLastTag();
            if (releaseData.Any()) {
                Data.Markdown = String.Empty;
                foreach (var release in releaseData) {
                    if (true) { // Temporary stand-in for version check
                    // if (string.Compare(MyApplication.CurrentVersion, release.tag_name) < 0) { // CurrentVersion is missing
                        Data.Markdown += $"# {release.tag_name}\n"; // Escaped newline
                        Data.Markdown += $"{release.body}\n"; // Escaped newline
                        Data.Markdown += $"# Assets\n"; // Escaped newline
                        foreach (var asset in release.assets) {
                            Data.Markdown += $"* [{asset.name}]({asset.browser_download_url})\n"; // Escaped newline
                        }
                        Data.Markdown += $"---\n"; // Escaped newline
                    }
                }
                if (string.IsNullOrEmpty(Data.Markdown)) {
                    // Data.Markdown = $"# {MyApplication.Lang.Releases.NoUpdated}"; // Lang is missing
                    Data.Markdown = $"# {"No New Updates Available"}"; // Temporary
                }
            }
        }
        public class ReleaseData : INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            // public string Markdown { get; set; } = $"# {MyApplication.Lang.Releases.Loading}"; // Lang is missing
            public string Markdown { get; set; } = $"# {"Loading..."}"; // Temporary
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
