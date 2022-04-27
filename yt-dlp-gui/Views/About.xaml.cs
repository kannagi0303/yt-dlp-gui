using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace yt_dlp_gui.Views {
    /// <summary>
    /// About.xaml 的互動邏輯
    /// </summary>
    public partial class About : Window {
        public About() {
            InitializeComponent();
            version.Text = App.CurrentVersion;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            Hyperlink link = sender as Hyperlink;
            // 激活的是当前默认的浏览器
            var url = link.NavigateUri.AbsoluteUri;
            Debug.WriteLine(url);
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
