using System;
using System.Collections.Generic;
using System.IO;
using Libs.Yaml;
using System.Linq;
using System.Windows;
using yt_dlp_gui.Models;
using System.Threading.Tasks;
using yt_dlp_gui.Wrappers;
using System.Diagnostics;
using yt_dlp_gui.App; // Added for ThemeManager

namespace yt_dlp_gui { // Restored to match RootNamespace
    /// <summary>
    /// Interaction logic for App.xaml (though App.xaml is now deleted)
    /// </summary>
    public class App : Application { // Removed 'partial'
        public static string CurrentVersion = "2023.03.28";
        public static Lang Lang { get; set; } = new();

        public static string AppExe;
        public static string AppPath;
        public static string AppName;
        private void LoadPath() {
            AppExe = Environment.ProcessPath;
            if (AppExe != null) { // Guard against null ProcessPath
                AppPath = System.IO.Path.GetDirectoryName(AppExe) ?? string.Empty;
                AppName = System.IO.Path.GetFileNameWithoutExtension(AppExe) ?? string.Empty;
            } else {
                AppPath = string.Empty;
                AppName = string.Empty;
                // Log or handle the error that Environment.ProcessPath was null
                Debug.WriteLine("Warning: Environment.ProcessPath is null. App paths not initialized.");
            }
        }
        public static string Path(Folders type, params string[] pathpart) {
            if (string.IsNullOrEmpty(AppPath) && AppExe == null) { // Check if LoadPath failed or wasn't called
                 // Attempt to initialize paths if they are missing.
                 // This is a fallback, ideally LoadPath is called reliably at startup.
                var localApp = Application.Current as App;
                localApp?.LoadPath(); // Call instance method if available
                                      // Or, if LoadPath can be static and self-sufficient (might need AppPath to be settable even if AppExe is null)
                                      // For now, assume LoadPath is called from App.xaml.cs startup.
                                      // If AppPath is still null, we might have an issue.
                 if (string.IsNullOrEmpty(AppPath)) {
                    Debug.WriteLine("Warning: AppPath is still null or empty in Path method. Returning combined path without base AppPath.");
                     // Combine just pathpart, or throw, or return empty, depending on desired handling for this edge case.
                     if (pathpart.Length == 0) return string.Empty;
                     return System.IO.Path.Combine(pathpart);
                 }
            }

            List<string> parmas = new() { AppPath }; // AppPath should be non-null here if LoadPath worked

            parmas.AddRange(type switch {
                Folders.root => Array.Empty<string>(),
                Folders.bin => new[] { "bin" },
                Folders.configs => new[] { "configs" },
                Folders.temp => new[] { "temp" },
                _ => throw new NotImplementedException(),
            });

            parmas.AddRange(pathpart);
            var res = "";
            try {
                res = System.IO.Path.Combine(parmas.ToArray());
            } catch (ArgumentException ex) { // Catch specific exception for path combination
                Debug.WriteLine($"Error combining path: {ex.Message}. Params: {string.Join(", ", parmas)}");
                // Handle error, e.g., return empty string or re-throw
                return string.Empty;
            }
            return res;
        }
        public enum Folders {
            root, bin, configs, temp
        }

        protected override async void OnStartup(StartupEventArgs e) {
            base.OnStartup(e); // Call base method

            ThemeManager.Initialize(); // Initialize ThemeManager

            // var args = e.Args.ToList(); // This line seems unused, consider removing if not needed.
            // For now, I'll keep it commented as per original logic to minimize changes beyond the direct request.

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
} // Restored to match RootNamespace
