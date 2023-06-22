using RenameMusic.Properties;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace RenameMusic.Language
{
    public static class AppLanguage
    {
        public static readonly Dictionary<string, string> Languages = new()
        {
            { "en", "English" },
            { "es", "Español" }
        };

        public static void ChangeLanguage(string lang)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
            Settings.Default.Lang = lang;
            Settings.Default.Save();
        }
    }
}
