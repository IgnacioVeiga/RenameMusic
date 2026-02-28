using RenameMusic.Properties;
using System.Windows;

namespace RenameMusic.Services
{
    /// <summary>
    /// Applies and persists UI theme selection using the app resource dictionaries.
    /// </summary>
    internal static class ThemeService
    {
        private const string DarkThemeName = "Dark";
        private const string LightThemeName = "Light";

        internal static void LoadTheme()
        {
            string normalizedThemeName = NormalizeThemeName(Settings.Default.ThemeName);
            if (!string.Equals(Settings.Default.ThemeName, normalizedThemeName, StringComparison.Ordinal))
            {
                Settings.Default.ThemeName = normalizedThemeName;
                Settings.Default.Save();
            }

            ResourceDictionary ThemeResDic = new()
            {
                Source = new Uri($"pack://application:,,,/Resources/Styles/{normalizedThemeName}.xaml")
            };
            ResourceDictionary UIResDic = new()
            {
                Source = new Uri("pack://application:,,,/Resources/Styles/UI/Generic.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(ThemeResDic);
            Application.Current.Resources.MergedDictionaries.Add(UIResDic);
            Application.Current.MainWindow?.UpdateLayout();
        }

        internal static void ChangeTheme(string themeName)
        {
            Settings.Default.ThemeName = NormalizeThemeName(themeName);
            Settings.Default.Save();
            LoadTheme();
        }

        private static string NormalizeThemeName(string? themeName)
        {
            return string.Equals(themeName, LightThemeName, StringComparison.OrdinalIgnoreCase)
                ? LightThemeName
                : DarkThemeName;
        }
    }
}
