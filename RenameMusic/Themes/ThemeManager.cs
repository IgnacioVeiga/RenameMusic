using RenameMusic.Properties;
using System;
using System.Windows;
using WinCopies.Util;

namespace RenameMusic.Themes
{
    internal static class ThemeManager
    {
        public static readonly string[] Themes = { "Dark", "Light" };

        internal static void LoadTheme()
        {
            ResourceDictionary ThemeResDic = new()
            {
                Source = new Uri($"pack://application:,,,/Themes/{Settings.Default.ThemeName}.xaml")
            };
            ResourceDictionary UIResDic = new()
            {
                Source = new Uri("pack://application:,,,/Themes/UI/Generic.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.AddRange(ThemeResDic, UIResDic);
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
