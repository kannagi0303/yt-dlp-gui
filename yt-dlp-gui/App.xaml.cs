using System.Linq;
using System.Windows;

namespace yt_dlp_gui {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static string CurrentVersion = "2022.09.17"; 
        private void Application_Startup(object sender, StartupEventArgs e) {
            var args = e.Args.ToList();
            LoadPath();

            new Views.Main().Show();
        }
    }
}
