using System.Windows;
using System.Windows.Controls;

namespace yt_dlp_gui.Controls {
    public class BrowserBehavior {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d) {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value) {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is WebBrowser wb) { // Use 'is' for type check and cast
                string? htmlContent = e.NewValue as string;
                wb.NavigateToString(htmlContent ?? string.Empty); // Pass empty string if null
            }
        }
    }
}
