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
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            Hyperlink link = sender as Hyperlink;
            // 激活的是当前默认的浏览器
            Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
