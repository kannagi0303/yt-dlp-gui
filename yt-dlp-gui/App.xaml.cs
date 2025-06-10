using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using yt_dlp_gui.Models; // ADDED
using yt_dlp_gui.Views;
using yt_dlp_gui.Wrappers; // ADDED

namespace yt_dlp_gui
{
    public partial class App : Application
    {
        public static Config AppConfig { get; set; } = new();
        public static Lang AppLang { get; set; } = new();
        public static ytDlp ytDlp { get; set; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CheckAppData();
            AppConfig = Config.Load();
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            InitTheme();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            MessageBox.Show(exception?.Message, "Error:CurrentDomain");
        }
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            var exception = e.Exception as Exception;
            MessageBox.Show(exception?.Message, "Error:TaskScheduler");
        }
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var exception = e.Exception as Exception;
            MessageBox.Show(exception?.Message, "Error:Dispatcher");
        }

        public static string AppData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "yt-dlp-gui");
        public static string PathConfig = Path.Combine(AppData, "config.json");
        public static string PathLangs = Path.Combine(AppData, "langs");
        public static string PathNotify = Path.Combine(AppData, "notify.wav");
        public static string PathDlp = Path.Combine(AppData, "yt-dlp.exe");
        public static string PathAria2c = Path.Combine(AppData, "aria2c.exe");
        public static string PathFFmpeg = Path.Combine(AppData, "ffmpeg.exe");
        public static string PathFFprobe = Path.Combine(AppData, "ffprobe.exe");
        public static string PathTEMP = Path.Combine(Path.GetTempPath(), "yt-dlp-gui");

        public static void CheckAppData()
        {
            if (!Directory.Exists(AppData)) Directory.CreateDirectory(AppData);
            if (!Directory.Exists(PathLangs)) Directory.CreateDirectory(PathLangs);
            if (!Directory.Exists(PathTEMP)) Directory.CreateDirectory(PathTEMP);
        }

        public ResourceDictionary ThemeDictionary
        {
            get { return Resources.MergedDictionaries[0]; }
        }
        public void InitTheme()
        {
            ThemeDictionary.MergedDictionaries.Clear();
            ThemeDictionary.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("Resources/Styles/Style.xaml", UriKind.Relative) });
            ThemeDictionary.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri($"Resources/Languages/{AppConfig.Lang}.xaml", UriKind.Relative) });
            ChangeTheme(AppConfig.Theme);
        }
        public void ChangeTheme(Theme theme)
        {
            AppConfig.Theme = theme;
            ThemeDictionary.MergedDictionaries[0] = new ResourceDictionary() { Source = new Uri($"Resources/Styles/{theme}.xaml", UriKind.Relative) };
        }
    }
}
