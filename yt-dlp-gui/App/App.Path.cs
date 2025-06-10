using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace yt_dlp_gui {
    using IoPath = System.IO.Path;
    public partial class App : System.Windows.Application { // Ensured it uses System.Windows.Application
        public static string AppExe;
        public static string AppPath;
        public static string AppName;
        private void LoadPath() {
            AppExe = Environment.ProcessPath;
            if (AppExe != null) { // Guard against null ProcessPath
                AppPath = IoPath.GetDirectoryName(AppExe) ?? string.Empty;
                AppName = IoPath.GetFileNameWithoutExtension(AppExe) ?? string.Empty;
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
                     return IoPath.Combine(pathpart);
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
                res = IoPath.Combine(parmas.ToArray());
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
    }
}
