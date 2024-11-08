using RenameMusic.Properties;
using System.Windows;

namespace RenameMusic.Services
{
    internal static class ThemeService
    {
        public static readonly string[] Themes = { "Dark", "Light" };

        internal static void LoadTheme()
        {
            ResourceDictionary ThemeResDic = new()
            {
                Source = new Uri($"pack://application:,,,/Resources/Styles/{Settings.Default.ThemeName}.xaml")
            };
            ResourceDictionary UIResDic = new()
            {
                Source = new Uri("pack://application:,,,/Resources/Styles/UI/Generic.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Clear();
            //Application.Current.Resources.MergedDictionaries.AddRange(ThemeResDic, UIResDic);
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
