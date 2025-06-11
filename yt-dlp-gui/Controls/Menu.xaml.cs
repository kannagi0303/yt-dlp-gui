using Libs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace yt_dlp_gui.Controls {
    /// <summary>
    /// Menu.xaml 的互動邏輯
    /// </summary>
    public partial class Menu : ContextMenu {
        public Menu() {
            InitializeComponent();
        }
        public static Menu Create(MenuDataItem item, FrameworkElement? target = null, MenuPlacement placement = MenuPlacement.BottomRight) {
            return Create(item.Items, target, placement);
        }
        public static Menu Create(IEnumerable<MenuDataItem> menu, FrameworkElement? target = null, MenuPlacement placement = MenuPlacement.BottomRight) {
            var m = new Menu();
            if (menu.Any()) {
                m.ItemsSource = menu;
                if (target != null) {
                    switch (placement) {
                        case MenuPlacement.BottomRight:
                            m.Placement = PlacementMode.Left;
                            m.HorizontalOffset = target.ActualWidth + 6;
                            m.VerticalOffset = target.ActualHeight;
                            break;
                        case MenuPlacement.Left:
                            m.Placement = PlacementMode.Left;
                            break;
                        case MenuPlacement.Right:
                            m.Placement = PlacementMode.Right;
                            break;
                        case MenuPlacement.TopLeft:
                            m.Placement = PlacementMode.Top;
                            break;
                        case MenuPlacement.BottomLeft:
                            m.Placement = PlacementMode.Bottom;
                            break;
                    }
                    m.PlacementTarget = target;
                    if (target is ToggleButton) {
                        var toggle = (ToggleButton)target;
                        m.Closed += (s, e) => toggle.IsChecked = false;
                    }
                }
            }
            return m;
        }
        public static Menu? Open(MenuDataItem item, FrameworkElement? target = null, MenuPlacement placement = MenuPlacement.BottomRight) {
            // If item or item.Items is null/empty, Create might return an empty menu.
            // Consider if Open should return Menu? if Create can effectively return a non-operable menu.
            // For now, matching Create's non-null return, but caller should be aware.
            return Open(item.Items, target, placement);
        }
        public static Menu? Open(IEnumerable<MenuDataItem> menu, FrameworkElement? target = null, MenuPlacement placement = MenuPlacement.BottomRight) {
            if (menu == null || !menu.Any()) return null; // Return null if menu is null or empty
            var m = Create(menu, target, placement);
            m.IsOpen = true;
            return m;
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            var item = (sender as FrameworkElement)?.DataContext as MenuDataItem; // Added null check for sender
            item?.Action?.Invoke();
        }
    }
    public enum MenuType {
        normal, title, separator
    }
    public enum MenuPlacement {
        Left, Right, TopLeft, TopRight, BottomLeft, BottomRight
    }
    public class MenuSeparator : MenuDataItem {
        public MenuSeparator() : base("") {
            Type = MenuType.separator;
        }
    }
    public class MenuTitle : MenuDataItem {
        public MenuTitle(string header) : base(header) {
            Type = MenuType.title;
        }
    }
    public class MenuDataItem {
        public IconKind Icon { get; set; } = IconKind.None;
        public Brush IconColor { get; set; } = Brushes.White; // Consider if this should be nullable if a "default" is not always White
        public object? Header { get; set; }
        public DataTemplate? HeaderTemplate { get; set; } = null;
        public Brush HeaderColor { get; set; } = Brushes.White; // Similar to IconColor
        public Action? Action { get; set; }
        public Window? Owner { get; set; } = null;
        public MenuDataItem(object? header = null, Action? execute = null) : this(header, IconKind.None, execute) { }
        public MenuDataItem(object? header, params MenuDataItem[] subitems) : this(header, IconKind.None, subitems) { }
        public MenuDataItem(object? header, IconKind icon, params MenuDataItem[] subitems) : this(header, icon) {
            // Ensure subitems are not null before adding if they can be.
            // The AddRange below already filters nulls within subitems but not subitems itself.
            if (subitems != null) {
                Items.AddRange(subitems);
            }
        }
        public MenuDataItem(object? header, IconKind icon, Action? execute = null) {
            Type = MenuType.normal;
            Icon = icon;
            Header = header;
            Action = execute;
        }
        public static List<MenuDataItem> Load<T>(DataTemplate? template, IEnumerable<T> col, Action<T> onclick) { // template can be null
            return col.Select(x => {
                var item = new MenuDataItem(x, () => onclick(x));
                item.HeaderTemplate = template; // item.HeaderTemplate is DataTemplate?
                return item;
            }).ToList();
        }
        public List<MenuDataItem> Items { get; set; } = new();
        public void Add(MenuDataItem item) => Items.Add(item);
        public void AddRange(IEnumerable<MenuDataItem> items) => Items.AddRange(items.Where(x => x != null));
        public void AddRange(MenuDataItem item) => Items.AddRange(item.Items);
        public bool HasItems => Items.Any();
        public MenuType Type { get; set; } = MenuType.normal;

        public static implicit operator MenuDataItem?(string? text) { // Allow null string to return null MenuDataItem
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text == "-" ? new MenuSeparator() : new MenuTitle(text);
        }
        // For tuple implicit operators, if any part of the tuple that maps to a non-nullable parameter
        // (after constructor changes) can be null, those parameters in constructors should be nullable too.
        // Assuming header from tuple is not null. If it can be, MenuDataItem constructors need header to be object?
        public static implicit operator MenuDataItem((string header, Action? execute) t) => new MenuDataItem(t.header, t.execute);
        public static implicit operator MenuDataItem((string header, MenuDataItem[] subitems) t) => new MenuDataItem(t.header, t.subitems);
        public static implicit operator MenuDataItem((string header, IconKind icon, Action? execute) t) => new MenuDataItem(t.header, t.icon, t.execute);
        public static implicit operator MenuDataItem((string header, IconKind icon, MenuDataItem[] subitems) t) => new MenuDataItem(t.header, t.icon, t.subitems);
    }
    
}
