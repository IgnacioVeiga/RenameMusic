using RenameMusic.Properties;
using System;
using System.Windows;

namespace RenameMusic.Themes
{
    internal static class ThemeManager
    {
        public static readonly string[] Themes = { "Dark", "Light" };

        internal static void LoadTheme()
        {
            string themeName = Settings.Default.ThemeName;
            string uri = $"pack://application:,,,/Themes/{themeName}.xaml";
            ResourceDictionary resDictionary = new() { Source = new Uri(uri) };
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(resDictionary);
            Application.Current.MainWindow.UpdateLayout();
        }

        internal static void ChangeTheme(string themeName)
        {
            Settings.Default.ThemeName = themeName;
            Settings.Default.Save();
            LoadTheme();
        }
    }
}
