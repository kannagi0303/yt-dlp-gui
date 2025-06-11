using ICSharpCode.AvalonEdit.Rendering;
using Libs;
using Newtonsoft.Json;
using Swordfish.NET.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Documents;
using yt_dlp_gui.Models;
using static yt_dlp_gui.Views.Main;

namespace yt_dlp_gui.Views {
    /// <summary>
    /// About.xaml 的互動邏輯
    /// </summary>
    public partial class About :Window {
        private readonly AboutViewData Data = new();
        public About() {
            InitializeComponent();
            // version.Text = MyApplication.CurrentVersion; // CurrentVersion is missing
version.Text = "?.?.?"; // Temporary
            DataContext = Data;
            Data.Items.Clear();
            Data.Items.Add(new() { Label = "Authors" /* MyApplication.Lang.About.Authors */, Context = "カンナギ Kannagi" });
            Data.Items.Add(new() { Label = "Website" /* MyApplication.Lang.About.Website */, Context = "https://github.com/Kannagi0303/yt-dlp-gui" });
            // foreach (var aitem in MyApplication.Lang.About.Extends) // Lang is missing
            // {
            //     Data.Items.Add(new() { Label = aitem.Key, Context = aitem.Value });
            // }
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
        private class AboutViewData :INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public ConcurrentObservableCollection<AboutItem> Items { get; set; } = new();
            public IEnumerable<AboutItem> ItemsView => Items.CollectionView;
        }
        private class AboutItem :INotifyPropertyChanged {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Label { get; set; } = string.Empty;
            public string Context { get; set; }= string.Empty;

        }
    }
}
