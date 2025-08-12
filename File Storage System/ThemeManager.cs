// ThemeManager.cs
// This class handles loading and applying theme resource dictionaries.

using System;
using System.Windows;

namespace FileFlow
{
    public static class ThemeManager
    {
        public enum Theme { Light, Dark }

        public static void ApplyTheme(Theme theme)
        {
            Application.Current.Resources.MergedDictionaries.Clear();
            var themeUri = theme == Theme.Dark
                ? new Uri("DarkTheme.xaml", UriKind.Relative)
                : new Uri("LightTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
    }
}
