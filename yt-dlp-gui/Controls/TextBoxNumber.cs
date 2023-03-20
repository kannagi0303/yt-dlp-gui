using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace yt_dlp_gui.Controls.Behaviors {
    public class TextBoxNumber : Behavior<TextBox> {
        public static readonly DependencyProperty NumberProperty = DependencyProperty.RegisterAttached(
            "Number", typeof(decimal), typeof(TextBoxNumber), new FrameworkPropertyMetadata(decimal.Zero, 
                FrameworkPropertyMetadataOptions.AffectsRender
                | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
                onNumberChanged, new CoerceValueCallback(CoerceNumber)));
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.RegisterAttached(
            "MinValue", typeof(decimal), typeof(TextBoxNumber), new PropertyMetadata(decimal.MinValue));
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.RegisterAttached(
            "MaxValue", typeof(decimal), typeof(TextBoxNumber), new PropertyMetadata(decimal.MaxValue));
        public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.RegisterAttached(
            "IsEmpty", typeof(bool), typeof(TextBoxNumber), new FrameworkPropertyMetadata(true));
        public bool IsEmpty {
            get => (bool)GetValue(IsEmptyProperty);
            set => SetValue(IsEmptyProperty, value);
        }
        public static object CoerceNumber(DependencyObject d, object value) {
            var t = (TextBoxNumber)d;
            var v = (decimal)value;
            if (v != 0 && v < t.MinValue) return t.MinValue;
            if (v != 0 && v > t.MaxValue) return t.MaxValue;
            return v;
        }
        private static void onNumberChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e) {
            var t = (dpo as TextBoxNumber);
            t.NumberToText();
        }
        private void NumberToText() {
            if (Number == 0 && !AssociatedObject.IsFocused) {
                AssociatedObject.Text = "なし";
                IsEmpty = true;
            } else {
                AssociatedObject.Text = Number.ToString();
                IsEmpty = false;
            }
        }
        public decimal Number {
            get => (decimal)GetValue(NumberProperty);
            set => SetValue(NumberProperty, value);
        }
        public decimal MinValue {
            get => (decimal)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }
        public decimal MaxValue {
            get => (decimal)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }
        protected override void OnAttached() {
            AssociatedObject.TextAlignment = TextAlignment.Center;
            NumberToText();
            InputMethod.SetIsInputMethodEnabled(AssociatedObject, false);
            DataObject.AddPastingHandler(AssociatedObject, TextBoxNumber_Pasting);
            AssociatedObject.PreviewKeyDown += TextBoxNumber_PreviewKeyDown;
            AssociatedObject.PreviewTextInput += TextBoxNumber_PreviewTextInput;
            AssociatedObject.PreviewMouseLeftButtonDown += TextBoxNumber_PreviewMouseLeftButtonDown;
            AssociatedObject.MouseWheel += TextBoxNumber_MouseWheel;
            AssociatedObject.LostFocus += TextBoxNumber_LostFocus;
            AssociatedObject.GotFocus += TextBoxNumber_GotFocus;

            base.OnAttached();
        }
        protected override void OnDetaching() {
            AssociatedObject.PreviewKeyDown -= TextBoxNumber_PreviewKeyDown;
            AssociatedObject.PreviewTextInput -= TextBoxNumber_PreviewTextInput;
            AssociatedObject.PreviewMouseLeftButtonDown -= TextBoxNumber_PreviewMouseLeftButtonDown;
            AssociatedObject.MouseWheel -= TextBoxNumber_MouseWheel;
            AssociatedObject.LostFocus -= TextBoxNumber_LostFocus;
            AssociatedObject.GotFocus -= TextBoxNumber_GotFocus;
            base.OnDetaching();
        }

        private void TextBoxNumber_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (e.Delta != 0) {
                NumberPlus(e.Delta > 0 ? 1 : -1);
            }
        }
        //輸入
        private void TextBoxNumber_PreviewKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    InputDone();
                    AssociatedObject.SelectAll();
                    break;
                case Key.Up:
                case Key.Down:
                    NumberPlus(e.Key == Key.Up ? 1 : -1);
                    AssociatedObject.Select(AssociatedObject.Text.Length, 0);
                    break;
            }
        }
        private void NumberPlus(decimal v) {
            var n = Number + v;
            if (n <= MinValue) n = MinValue;
            if (n >= MaxValue) n = MaxValue;
            Number = n;
        }
        //取得焦點 - 滑鼠
        private void TextBoxNumber_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (!AssociatedObject.IsKeyboardFocusWithin) {
                e.Handled = true;
                AssociatedObject.Focus();
            }
        }
        //取得焦點 - 鍵盤
        private void TextBoxNumber_GotFocus(object sender, RoutedEventArgs e) {
            NumberToText();
            AssociatedObject.SelectAll();
        }
        //失去焦點
        private void TextBoxNumber_LostFocus(object sender, RoutedEventArgs e) {
            InputDone();
            NumberToText();
            AssociatedObject.Select(AssociatedObject.Text.Length, 0);
        }
        //完成處理
        private void InputDone() {
            decimal n = decimal.Zero;
            if (!string.IsNullOrWhiteSpace(AssociatedObject.Text)) {
                if (decimal.TryParse(AssociatedObject.Text, out decimal tryValue)) {
                    n = tryValue;
                }
            }
            Number = n;
        }
        //驗證數字 - 貼上
        private void TextBoxNumber_Pasting(object sender, DataObjectPastingEventArgs e) {
            if (e.DataObject.GetDataPresent(typeof(string))) {
                var text = e.DataObject.GetData(typeof(string)) as string;
                if (!IsVaild(text)) e.CancelCommand();
            }
        }
        //驗證數字 - 輸入
        private void TextBoxNumber_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) {
            var full = AssociatedObject.Text + e.Text;
            e.Handled = !IsVaild(full);
        }
        //驗證數字
        private static readonly Regex patten = new Regex(@"^[0-9]+\.?[0-9]*$");
        private bool IsVaild(string str) {
            return patten.IsMatch(str);
        }
    }
}
