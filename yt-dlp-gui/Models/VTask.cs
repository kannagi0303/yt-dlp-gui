using Swordfish.NET.Collections;
using Swordfish.NET.Collections.Auxiliary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace yt_dlp_gui.Models {
    public class VMain :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public List<Config> Configs { get; set; } = new List<Config>();
        public Config SelectedConfig { get; set; } = new Config();

    }
    public class VTask :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public VTaskVideo Video { get; set; } = new VTaskVideo();
        public VTask() { 
            
        }
    }
    public class VTaskVideo :INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public Video? Source { get; set; } = new();
        // Formats =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
        public IEnumerable<Format> Formats => getFormats(Source?.formats);
        public IEnumerable<Format> FormatsVideo => Formats.Where(x => x.type == FormatType.package || x.type == FormatType.video).OrderBy(x => x, ComparerVideo.Comparer);
        public IEnumerable<Format> FormatsAudio => Formats.Where(x => x.type == FormatType.package || x.type == FormatType.audio).OrderBy(x => x, ComparerAudio.Comparer);
        public IEnumerable<Format> RequestedFormats => getFormats(Source?.requested_formats);
        private IEnumerable<Format> getFormats(IEnumerable<Format>? source) {
            return source?.Select(row => {
                if (row.vcodec != "none" && row.acodec != "none") {
                    row.type = FormatType.package;
                    if (row.height.HasValue && row.width.HasValue) {
                        row.resolution = $"{row.width.Value}x{row.height.Value}";
                    }
                } else if (row.vcodec != "none") {
                    row.type = FormatType.video;
                    if (row.height.HasValue && row.width.HasValue) {
                        row.resolution = $"{row.width.Value}x{row.height.Value}";
                    }
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
                return row;
            }).ToList() ?? new List<Format>();
        }

        // Chapters =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
        public IEnumerable<Chapters> Chapters =>
            (Source?.chapters?.Any() ?? false
            ? new[] {
                new Chapters() { title = App.Lang.Main.ChaptersAll, type = ChaptersType.None },
                new Chapters() { title = App.Lang.Main.ChaptersSplite, type = ChaptersType.Split },
            }.Concat(Source.chapters)
            : new[] {
                new Chapters() { title = App.Lang.Main.ChaptersNone, type = ChaptersType.None },
            }).ToList();
    }
}
