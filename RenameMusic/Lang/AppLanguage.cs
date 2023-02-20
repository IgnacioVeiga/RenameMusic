using RenameMusic.Properties;
using System.Globalization;
using System.Threading;

namespace RenameMusic.Lang
{
    internal static class AppLanguage
    {
        // ToDo: Try to generate the ComboBoxItems of the language switcher from this enum
        internal enum Languages
        {
            English = 0,
            Español = 1
        }

        /// <summary>
        /// 0 = English
        /// 1 = Español
        /// </summary>
        /// <param name="langIndex"></param>
        internal static void ChangeLanguage(int langIndex)
        {
            string lang = "en";

            switch (langIndex)
            {
                case (int)Languages.Español:
                    lang = "es";
                    break;
            }

            Settings.Default.LangIndex = langIndex;
            Settings.Default.Save();
            Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
        }
    }
}
