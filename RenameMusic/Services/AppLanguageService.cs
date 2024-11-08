using RenameMusic.Properties;
using System.Globalization;

namespace RenameMusic.Services
{
    public static class AppLanguageService
    {
        public static readonly Dictionary<string, string> Languages = new()
        {
            { "en", "English" },
            { "es", "Español" }
        };

        public static void ChangeLanguage(string? lang)
        {
            lang = (lang is null) ? "en" : lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
            Settings.Default.Language = lang;
            Settings.Default.Save();
        }
    }
}
