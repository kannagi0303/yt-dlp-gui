using Microsoft.Xaml.Behaviors;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace yt_dlp_gui.Controls {
    public class ActualSize : Behavior<FrameworkElement> {
        // ActualWidth -_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_
        private static readonly DependencyProperty ActualWidthProperty = DependencyProperty.RegisterAttached(
            "ActualWidth", typeof(double), typeof(ActualSize),
            // Mode=OneWayToSource
            new FrameworkPropertyMetadata() {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        // ActualHeight -_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_
        private static readonly DependencyProperty ActualHeightProperty = DependencyProperty.RegisterAttached(
            "ActualHeight", typeof(double), typeof(ActualSize),
            // Mode=OneWayToSource
            new FrameworkPropertyMetadata() {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        // ActualSize -_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
        }
        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
        }
        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e) {
            ActualWidth = AssociatedObject.ActualWidth;
            ActualHeight = AssociatedObject.ActualHeight;
        }
        public double ActualWidth {
            get => (double)GetValue(ActualWidthProperty);
            set => SetValue(ActualWidthProperty, value);
        }
        public double ActualHeight {
            get => (double)GetValue(ActualHeightProperty);
            set => SetValue(ActualHeightProperty, value);
        }
    }
}
