using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yt_dlp_gui.Wrappers {
    public class FFMPEG {
        public static string Path_FFMPEG { get; set; } = string.Empty;
        public static void Merger(bool overwrite, string target, params string[] sources) {
            var options = new List<string>();
            foreach (var source in sources) {
                options.Add($"-i \"{source}\"");
            }
            options.Add("-vcodec copy");
            options.Add("-acodec copy");
            if (Path.GetExtension(target).ToLower() == ".mp4") options.Add("-c:s mov_text");
            if (overwrite) {
                options.Add("-y");
            } else {
                options.Add("-n");
            }
            options.Add($"\"{target}\"");
            var args = string.Join(" ", options);
            Debug.WriteLine(args);
            Exec(args);
        }
        public static void DownloadUrl(string url, string target) {
            var options = new List<string>();
            options.Add($"-i \"{url}\"");
            options.Add("-y");
            options.Add($"\"{target}\"");
            var args = string.Join(" ", options);
            Exec(args);
        }
        private static void Exec(string args = "") {
            //var fn = App.Path(App.Folders.bin, "ffmpeg.exe");
            var fn = Path_FFMPEG;
            Process p = new Process();
            p.StartInfo.FileName = fn;
            p.StartInfo.Arguments = args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += (s, e) => {
                //Debug.WriteLine(e.Data, "STD");
            };
            p.ErrorDataReceived += (s, e) => {
                //Debug.WriteLine(e.Data, "ERR");
            };
            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            p.WaitForExit();
        }
    }
}
