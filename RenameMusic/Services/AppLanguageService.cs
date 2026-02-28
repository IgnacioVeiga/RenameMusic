using RenameMusic.Properties;
using System.Globalization;

namespace RenameMusic.Services
{
    /// <summary>
    /// Applies UI language changes and persists the selected app language.
    /// </summary>
    public static class AppLanguageService
    {
        private const string EnglishLanguageCode = "en";
        private const string SpanishLanguageCode = "es";

        public static void ChangeLanguage(string? lang)
        {
            string normalizedLanguage = NormalizeLanguageCode(lang);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(normalizedLanguage);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(normalizedLanguage);
            Settings.Default.Language = normalizedLanguage;
            Settings.Default.Save();
        }

        private static string NormalizeLanguageCode(string? lang)
        {
            return string.Equals(lang, SpanishLanguageCode, StringComparison.OrdinalIgnoreCase)
                ? SpanishLanguageCode
                : EnglishLanguageCode;
        }
    }
}
