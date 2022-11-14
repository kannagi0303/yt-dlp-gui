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
                    if (string.Compare(App.CurrentVersion, release.tag_name) < 0) {
                        Data.Markdown += $"# {release.tag_name}\n";
                        Data.Markdown += $"{release.body}\n";
                        Data.Markdown += $"# Assets\n";
                        foreach (var asset in release.assets) {
                            Data.Markdown += $"* [{asset.name}]({asset.browser_download_url})\n";
                        }
                        Data.Markdown += $"---\n";
                    }
                }
                if (string.IsNullOrEmpty(Data.Markdown)) {
                    Data.Markdown = $"# {App.Lang.Releases.NoUpdated}";
                }
            }
        }
        public class ReleaseData : INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Markdown { get; set; } = $"# {App.Lang.Releases.Loading}";
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
