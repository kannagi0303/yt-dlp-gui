using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Windows;
using System.Windows.Controls;

namespace yt_dlp_gui.Controls {
    /// <summary>
    /// TextEditor.xaml 的互動邏輯
    /// </summary>
    public partial class TextEditor : UserControl, ITextView {
        public static readonly DependencyProperty MultilineProperty = DependencyProperty.RegisterAttached(
            "Multiline", typeof(bool), typeof(TextEditor),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty WordWrapProperty = DependencyProperty.RegisterAttached(
            "WordWrap", typeof(bool), typeof(TextEditor),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty HelperProperty = DependencyProperty.RegisterAttached(
            "Helper", typeof(string), typeof(TextEditor),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.RegisterAttached(
            "IsReadOnly", typeof(bool), typeof(TextEditor),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty EnableHyperlinksProperty = DependencyProperty.RegisterAttached(
            "EnableHyperlinks", typeof(bool), typeof(TextEditor),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, onEnableHyperlinksChangedChanged));
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(TextEditor), 
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, onTextChanged));
        public static readonly DependencyProperty SyntaxProperty = DependencyProperty.RegisterAttached(
            "Syntax", typeof(string), typeof(TextEditor),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, onSyntaxChanged));
        private static void onTextChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var d = (dpo as TextEditor);
            if (d != null) {
                var editor = d.textEditor;
                if (editor.Document != null) {
                    var caretOffset = editor.CaretOffset;
                    editor.Document.Text = (string)e.NewValue ?? "";

                    var length = editor.Document.Text.Length;
                    if (caretOffset > length) caretOffset = length;
                    editor.CaretOffset = caretOffset;
                }
            }
        }
        private static void onSyntaxChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var d = (dpo as TextEditor);
            var n = e.NewValue?.ToString() ?? "";
            d?.LoadDefinition(n);
        }
        private static void onEnableHyperlinksChangedChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var d = (dpo as TextEditor);
            if (d != null) d.textEditor.Options.EnableHyperlinks = (bool)e.NewValue;
        }
        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public string Syntax {
            get => (string)GetValue(SyntaxProperty);
            set => SetValue(SyntaxProperty, value);
        }
        
        public string Helper {
            get => (string)GetValue(HelperProperty);
            set => SetValue(HelperProperty, value);
        }
        public bool WordWrap {
            get => (bool)GetValue(WordWrapProperty);
            set => SetValue(WordWrapProperty, value);
        }
        public bool Multiline {
            get => (bool)GetValue(MultilineProperty);
            set => SetValue(MultilineProperty, value);
        }
        public bool IsReadOnly {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }
        
        public bool EnableHyperlinks {
            get => (bool)GetValue(EnableHyperlinksProperty);
            set => SetValue(EnableHyperlinksProperty, value);
        }
        public IHighlightingDefinition SyntaxDefinition {
            set {
                textEditor.SyntaxHighlighting = value;
                //textEditor.TextArea.TextView.LineTransformers.Insert(0, new HighlightingColorizer(value));
            }
        }

        public TextEditor() {
            InitializeComponent();
            textEditor.Document = new TextDocument();
            textEditor.TextChanged += TextEditor_TextChanged;
            textEditor.Options.EnableHyperlinks = EnableHyperlinks;
            //textEditor.Options.HighlightCurrentLine = true;
            textEditor.PreviewMouseLeftButtonDown += TextEditor_PreviewMouseLeftButtonDown;
            textEditor.GotFocus += TextEditor_GotFocus;
            textEditor.LostFocus += TextEditor_LostFocus;
        }

        private void TextEditor_LostFocus(object sender, RoutedEventArgs e) {
            if (!Multiline) {
                textEditor.Select(Text.Length, 0);
            }
        }

        private void TextEditor_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (!Multiline) {
                if (!textEditor.IsKeyboardFocusWithin) {
                    e.Handled = true;
                    textEditor.Focus();
                }
            }
        }

        private void TextEditor_GotFocus(object sender, RoutedEventArgs e) {
            if (!Multiline) textEditor.SelectAll();
        }

        private void TextEditor_TextChanged(object? sender, EventArgs e) {
            var t = sender as ICSharpCode.AvalonEdit.TextEditor;
            if (t != null) {
                if (t.Document != null) Text = t.Document.Text;
            }
        }
    }
}
