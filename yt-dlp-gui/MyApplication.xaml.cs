using System.Windows;
using yt_dlp_gui.Models;

namespace yt_dlp_gui
{
    public partial class MyApplication : Application // Changed class name here
    {
        public static Lang AppLang { get; } = new Lang();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Further initialization will be added here in Phase 2,
            // calling out to helper classes, once this minimal version builds successfully.
        }
    }
}
