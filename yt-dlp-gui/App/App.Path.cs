// Content temporarily removed for diagnostic purposes to trace CS0101 error for App class.
// Original content included a partial class App with static path members.
/*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace yt_dlp_gui {
    using IoPath = System.IO.Path;
    public partial class App : System.Windows.Application {
        public static string AppExe;
        public static string AppPath;
        public static string AppName;
        private void LoadPath() {
            AppExe = Environment.ProcessPath;
            AppPath = IoPath.GetDirectoryName(AppExe);
            AppName = IoPath.GetFileNameWithoutExtension(AppExe);
        }
        public static string Path(Folders type, params string[] pathpart) {
            List<string> parmas = new() { AppPath };
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
            } catch (Exception) { }
            return res;
        }
        public enum Folders {
            root, bin, configs, temp
        }
    }
}
*/
