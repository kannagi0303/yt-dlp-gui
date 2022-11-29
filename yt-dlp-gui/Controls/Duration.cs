using System;
using System.Windows;
using System.Windows.Controls;

namespace yt_dlp_gui.Controls {
    public abstract class Duration {
        // Visible -_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_
        public static readonly DependencyProperty SecsProperty
            = DependencyProperty.RegisterAttached("Secs", typeof(double?), typeof(Duration),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SecsChanged));
        private static void SecsChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var (d, v) = (dpo as TextBlock, GetSecs(dpo));
            if (v.HasValue) {
                TimeSpan ts = TimeSpan.FromSeconds(v.Value);
                if (ts.Days > 0) {
                    d.Text = ts.ToString("d'.'hh':'mm':'ss");
                } else if (ts.Hours > 0) {
                    d.Text = ts.ToString("h':'mm':'ss");
                } else {
                    d.Text = ts.ToString("mm':'ss");
                }
            } else {
                d.Text = "";
            }
        }
        public static void SetSecs(DependencyObject dpo, double? value)
            => dpo.SetValue(SecsProperty, value);
        public static double? GetSecs(DependencyObject dpo)
            => (double?)dpo.GetValue(SecsProperty);
         
    }
}
