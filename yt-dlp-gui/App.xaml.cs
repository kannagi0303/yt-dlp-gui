using Libs.Yaml;
using System.Linq;
using System.Windows;
using yt_dlp_gui.Models;
using System.Threading.Tasks;
using yt_dlp_gui.Wrappers;
using System.Diagnostics;
using yt_dlp_gui.App; // Added for ThemeManager


namespace yt_dlp_gui {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static string CurrentVersion = "2023.03.28";
        public static Lang Lang { get; set; } = new();
        private async void Application_Startup(object sender, StartupEventArgs e) {
            ThemeManager.Initialize(); // Initialize ThemeManager

            var args = e.Args.ToList();

            try {
                await DLP.EnsureYtdlpPathAsync();
            } catch (Exception ex) {
                Debug.WriteLine($"Failed to ensure yt-dlp path on startup: {ex.Message}");
            }

            LoadPath();

            var langPath = App.Path(App.Folders.root, App.AppName + ".lang");
            Lang = Yaml.Open<Lang>(langPath);
            new Views.Main().Show();
        }
    }
}
