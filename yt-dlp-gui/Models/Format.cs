using Swordfish.NET.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace yt_dlp_gui.Models {
    public enum FormatType { video, audio, package, other }
    public class Format : INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public decimal? asr { get; set; } = null;
        public long? filesize { get; set; } = null; //bytes
        public string format_id { get; set; } = string.Empty;
        public string format_note { get; set; } = "";
        public decimal? fps { get; set; } = null;
        public decimal? height { get; set; } = null;
        public decimal? width { get; set; } = null;
        public string ext { get; set; } = string.Empty;
        public string vcodec { get; set; } = string.Empty;
        public string acodec { get; set; } = string.Empty;
        public string? dynamic_range { get; set; } = null;
        public decimal? tbr { get; set; } = null; //k
        public decimal? vbr { get; set; } = null; //k
        public decimal? abr { get; set; } = null; //k
        public decimal? preference { get; set; } = null;
        public string container { get; set; } = string.Empty;
        public string audio_ext { get; set; } = "none";
        public string video_ext { get; set; } = "none";
        public FormatType type { get; set; } = FormatType.other;
        public string format { get; set; } = string.Empty;
        public string resolution { get; set; } = string.Empty;
        public string info { get; set; } = string.Empty;
    }
    public class ComparerAudio : IComparer<Format> {
        public int Compare(Format? x, Format? y) {
            if (x == null && y == null) return 0;
            var r = 0;
            //比较 ABR
            if (x.abr.HasValue && y.abr.HasValue) {
                var max = Math.Max(x.abr.Value, y.abr.Value);
                var min = Math.Min(x.abr.Value, y.abr.Value);
                if (max != 0m) {
                    var e = 1m - min / max;
                    if (e > 0.1m) {
                        return x.abr.Value > y.abr.Value ? -1 : 1;
                    }
                }
            }
            //比较 ASR
            if (x.asr.HasValue && y.asr.HasValue) {
                return x.asr.Value > y.asr.Value ? -1 : 1;
            }

            return 0;
        }
        public static ComparerAudio Comparer = new ComparerAudio();
    }
    public class ComparerVideo : IComparer<Format> {
        public int Compare(Format? x, Format? y) {
            if (x == null && y == null) return 0;
            var r = 0;
            //比较 resolution
            if (x.height.HasValue && y.height.HasValue) {
                var xr = x.width.Value * x.height.Value;
                var yr = y.width.Value * y.height.Value;
                if (xr != yr) {
                    return xr > yr ? -1 : 1;
                }
            }
            //比较 vbr
            if (x.vbr.HasValue && y.vbr.HasValue) {
                var max = Math.Max(x.vbr.Value, y.vbr.Value);
                var min = Math.Min(x.vbr.Value, y.vbr.Value);
                if (max != 0m) {
                    var e = 1m - min / max;
                    if (e > 0.1m) {
                        return x.vbr.Value > y.vbr.Value ? -1 : 1;
                    }
                }
            }
            //比较 格式
            var prefer = new List<string>() { "VP9", "AV1", "H.264" };
            var xf = prefer.IndexOf(x.vcodec);
            var yf = prefer.IndexOf(y.vcodec);
            if (xf != yf) return xf > yf ? -1 : 1;

            return 0;
        }
        public static ComparerVideo Comparer = new ComparerVideo();
    }
    public static class ExtensionFormat {
        public static void LoadFromVideo(this ConcurrentObservableCollection<Format> source, Video from) {
            foreach (var row in from.formats) {
                //分类
                if (row.vcodec != "none" && row.acodec != "none") {
                    row.type = FormatType.package;
                } else if (row.vcodec != "none") {
                    row.type = FormatType.video;
                } else if (row.acodec != "none") {
                    row.type = FormatType.audio;
                } else {
                    row.type = FormatType.other;
                }
                //Video Codec
                if (row.vcodec.StartsWith("vp9", StringComparison.InvariantCultureIgnoreCase)) {
                    row.vcodec = "VP9";
                } else if (row.vcodec.StartsWith("av01", StringComparison.InvariantCultureIgnoreCase)) {
                    row.vcodec = "AV1";
                } else if (row.vcodec.StartsWith("avc", StringComparison.InvariantCultureIgnoreCase)) {
                    row.vcodec = "H.264";
                }
                //Audio Codec
                if (row.acodec.StartsWith("mp4a", StringComparison.InvariantCultureIgnoreCase)) {
                    row.acodec = "AAC";
                } else if (row.acodec.StartsWith("opus", StringComparison.InvariantCultureIgnoreCase)) {
                    row.acodec = "OPUS";
                }
            }
            source.Reset(from.formats);
        }
    }
}
