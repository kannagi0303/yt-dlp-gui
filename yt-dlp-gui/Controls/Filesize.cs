using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace yt_dlp_gui.Controls {
    public enum FilesizeUnit : int {
        Auto = -1, B = 0, KB = 1, MB = 2, GB = 3, TB = 4, PB = 5, EB = 6, ZB = 7, YB = 8
    }
    public abstract class Filesize {
        // Visible -_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_
        public static readonly DependencyProperty BytesProperty
            = DependencyProperty.RegisterAttached("Bytes", typeof(long?), typeof(Filesize),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BytesPropertyChanged));
        public static readonly DependencyProperty UnitProperty
            = DependencyProperty.RegisterAttached("Unit", typeof(FilesizeUnit), typeof(Filesize),
                new FrameworkPropertyMetadata(FilesizeUnit.Auto, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BytesPropertyChanged));
        private static void BytesPropertyChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var (d, v) = (dpo as TextBlock, GetBytes(dpo));
            var value = v.HasValue ? v.Value : 0;
            
            //負數處理
            (value, bool IsNegative) = value < 0 
                ? (-value, true) 
                : (value, false);

            int decimalPlaces = 2; //小數點位數
            int mag = 0; //級數
            decimal adjustedSize = 0; //調整後的值
            if (value > 0) {
                var unit = GetUnit(dpo);
                var isAuto = unit == FilesizeUnit.Auto;
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
            d.Inlines.Clear();
            if (!IsNegative) {
                d.Inlines.Add(new Run(txt));
            } else {
                d.Inlines.Add(new Run("-" + txt) { Foreground = Brushes.OrangeRed });
            }
            var unitText = ((FilesizeUnit)mag).ToString().PadLeft(3, ' ');
            //d.Inlines.Add(new Run(unitText) { Foreground = BrushColors.Green });
            d.Inlines.Add(new Run(unitText));
        }
        public static void SetBytes(DependencyObject dpo, long? value)
            => dpo.SetValue(BytesProperty, value);
        public static long? GetBytes(DependencyObject dpo)
            => (long?)dpo.GetValue(BytesProperty);
        public static void SetUnit(DependencyObject dpo, FilesizeUnit value)
            => dpo.SetValue(UnitProperty, value);
        public static FilesizeUnit GetUnit(DependencyObject dpo)
            => (FilesizeUnit)dpo.GetValue(UnitProperty);
    }
}
