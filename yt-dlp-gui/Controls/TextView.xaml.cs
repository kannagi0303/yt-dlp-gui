using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Libs;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace yt_dlp_gui.Controls {
    /// <summary>
    /// TextEditor.xaml 的互動邏輯
    /// </summary>
    public partial class TextView : UserControl, ITextView {
        public static readonly DependencyProperty MultilineProperty = DependencyProperty.RegisterAttached(
            "Multiline", typeof(bool), typeof(TextView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(TextView), 
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, onTextChanged));
        public static readonly DependencyProperty SyntaxProperty = DependencyProperty.RegisterAttached(
            "Syntax", typeof(string), typeof(TextView),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, onSyntaxChanged));
        public static readonly DependencyProperty EnableHyperlinksProperty = DependencyProperty.RegisterAttached(
            "EnableHyperlinks", typeof(bool), typeof(TextView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, onLinkChanged));
        private static void onTextChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var d = (dpo as TextView);
            if (d != null) {
                var editor = d.textView;
                if (editor.Document != null) {
                    editor.Document.Text = (string)e.NewValue ?? "";
                }
            }
        }
        private static void onSyntaxChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var d = (dpo as TextView);
            var n = e.NewValue?.ToString() ?? "";
            //d?.LoadDefinition(n);
        }
        private static void onLinkChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var d = (dpo as TextView);
            d.textView.Options.EnableHyperlinks = d.EnableHyperlinks;
            d.textView.Options.RequireControlModifierForHyperlinkClick = !d.EnableHyperlinks;
        }
        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public string Syntax {
            get => (string)GetValue(SyntaxProperty);
            set => SetValue(SyntaxProperty, value);
        }
        public bool Multiline {
            get => (bool)GetValue(MultilineProperty);
            set => SetValue(MultilineProperty, value);
        }
        public bool EnableHyperlinks {
            get => (bool)GetValue(EnableHyperlinksProperty);
            set => SetValue(EnableHyperlinksProperty, value);
        }
        public IHighlightingDefinition SyntaxDefinition {
            set {
                textView.LineTransformers.Clear();
                textView.LineTransformers.Insert(0, new HighlightingColorizer(value)); 
            }
        }
        public TextView() {
            InitializeComponent();
            textView.Document = new TextDocument();
            textView.Options.EnableHyperlinks = EnableHyperlinks;
        }
    }
    public interface ITextView {
        public IHighlightingDefinition SyntaxDefinition { set; }
    }
    public static class TextViewExtensions {
        public static void LoadDefinition(this ITextView view, string name = "") {
            if (string.IsNullOrEmpty(name)) return;
            try {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"Resources/xshd/{name}.xshd";

                if (!Util.ResourceExists(resourceName)) return;
                using (Stream s = Application.GetResourceStream(new Uri(resourceName, UriKind.Relative)).Stream) {
                    using (XmlTextReader reader = new XmlTextReader(s)) {
                        var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        view.SyntaxDefinition = definition;
                    }
                }
            } catch (Exception) { }
            return;
        }
    }
}
