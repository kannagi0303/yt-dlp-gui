using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Libs {
    public partial class Util {
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        };
        private static HashSet<string> SpecalPaths = new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.Favorites),
                Environment.GetFolderPath(Environment.SpecialFolder.Recent),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.History),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            }.OrderByDescending(x => x.Length).ToHashSet();
        public static string ReplaceSpecialPath(string path) {
            var match = SpecalPaths.FirstOrDefault(x => path.StartsWith(x));
            if (!string.IsNullOrWhiteSpace(match)) {
                return path.Replace(match, GetDisplayName(match));
            }
            return path;
        }
        public static string GetDisplayName(string path) {
            SHFILEINFO shfi = new SHFILEINFO();
            if (0 != (int)SHGetFileInfo(path, FILE_ATTRIBUTE_NORMAL, out shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_DISPLAYNAME)) {
                return shfi.szDisplayName;
            }
            return null;
        }
    }
}
