using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Libs {
    public enum SizeUnit :int {
        Auto = -1, B = 0, K = 1, M = 2, G = 3, T = 4, P = 5, E = 6, Z = 7, Y = 8
    }
    public partial class Util {
        public static string SecToStr(decimal secs) {
            TimeSpan ts = TimeSpan.FromSeconds((double)secs);
            if (ts.Days > 0) {
                return ts.ToString("d'.'hh':'mm':'ss");
            } else if (ts.Hours > 0) {
                return ts.ToString("h':'mm':'ss");
            } else {
                return ts.ToString("mm':'ss");
            }
        }

        public static string GetAutoUnit(long value, SizeUnit unit = SizeUnit.Auto, string suffix = "B") {
            //負數處理
            (value, bool IsNegative) = value < 0
                ? (-value, true)
                : (value, false);

            int decimalPlaces = 2; //小數點位數
            int mag = 0; //級數
            decimal adjustedSize = 0; //調整後的值
            if (value > 0) {
                var isAuto = unit == SizeUnit.Auto;
                mag = isAuto
                    ? (int)Math.Log(value, 1024)
                    : (int)unit;

                adjustedSize = (decimal)value / (1L << (mag * 10));
                if (isAuto) {
                    if (Math.Round(adjustedSize, decimalPlaces) >= 1000) {
                        mag += 1;
                        adjustedSize /= 1024;
                    }
                }
            }
            var txt = adjustedSize.ToString("n" + decimalPlaces);
            var output = "";
            if (!IsNegative) {
                output += txt;
            } else {
                output += "-" + txt;
            }
            var unitText = ((SizeUnit)mag).ToString().PadLeft(2, ' ') + suffix;
            output += unitText;
            return output;
        }
        public static bool RemoveEmptyDirectories(string startLocation) {
            foreach (var directory in Directory.GetDirectories(startLocation)) {
                RemoveEmptyDirectories(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0) {
                    try {
                        Directory.Delete(directory, false);
                    } catch (Exception e) {
                        return false;
                    }
                }
            }
            return true;
        }
        public static Dictionary<string, string> GetGroup(Regex r, string input) {
            var m = r.Match(input);
            if (m.Success) {
                var groupData = r.GetGroupNames()
                    .Where(x => !string.IsNullOrWhiteSpace(m.Groups[x]?.Value))
                    .ToDictionary(x => x.ToLower(), x => m.Groups[x]);
                var group = groupData.ToDictionary(x => x.Key, x => x.Value.Value.Trim());
                return group;
            }
            return new Dictionary<string, string>();
        }
        public static void NotifySound(string path = "") {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) {
                path = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current")?.GetValue("")?.ToString() ?? "";
            }
            if (!string.IsNullOrWhiteSpace(path)) {
                new SoundPlayer(path).Play();
            }
        }
    }
}
