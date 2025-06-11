using System;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace yt_dlp_gui.App {
    public enum CurrentUiTheme {
        Light,
        Dark
    }

    public static class ThemeManager {
        public static CurrentUiTheme CurrentTheme { get; private set; } = CurrentUiTheme.Light; // Default to Light

        private static readonly Uri LightThemeUri = new Uri("../Themes/CustomUI.xaml", UriKind.Relative);
        private static readonly Uri DarkThemeUri = new Uri("../Themes/DarkTheme.xaml", UriKind.Relative);

        public static void Initialize() {
            Debug.WriteLine("ThemeManager: Initializing with default theme (Light).");
            ApplyTheme(CurrentUiTheme.Light);
        }

        public static void ApplyTheme(CurrentUiTheme theme) {
            Debug.WriteLine($"ThemeManager: Applying theme - {theme}");
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

            // Remove existing theme dictionaries
            var dictionariesToRemove = mergedDictionaries
                .Where(rd => rd.Source == LightThemeUri || rd.Source == DarkThemeUri)
                .ToList();

            foreach (var rd in dictionariesToRemove) {
                Debug.WriteLine($"ThemeManager: Removing existing theme dictionary: {rd.Source}");
                mergedDictionaries.Remove(rd);
            }

            ResourceDictionary newThemeDictionary = new ResourceDictionary();
            if (theme == CurrentUiTheme.Dark) {
                newThemeDictionary.Source = DarkThemeUri;
                CurrentTheme = CurrentUiTheme.Dark;
            } else {
                newThemeDictionary.Source = LightThemeUri; // Default to Light
                CurrentTheme = CurrentUiTheme.Light;
            }

            Debug.WriteLine($"ThemeManager: Adding new theme dictionary: {newThemeDictionary.Source}");
            mergedDictionaries.Add(newThemeDictionary);
            Debug.WriteLine($"ThemeManager: Current theme set to {CurrentTheme}");
        }

        public static void ToggleTheme() {
            Debug.WriteLine("ThemeManager: ToggleTheme called.");
            if (CurrentTheme == CurrentUiTheme.Light) {
                ApplyTheme(CurrentUiTheme.Dark);
            } else {
                ApplyTheme(CurrentUiTheme.Light);
            }
        }
    }
}
