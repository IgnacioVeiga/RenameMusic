using RenameMusic.Properties;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace RenameMusic.Lang
{
    public enum Languages
    {
        [Display(Name = "en")]
        English = 0,
        [Display(Name = "es")]
        Español = 1
    }

    public static class AppLanguage
    {
        public static void ChangeLanguage(string lang)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
            Settings.Default.Lang = lang;
            Settings.Default.Save();
        }
    }

    public static class EnumHelper
    {
        public static string GetDisplayValue<TEnum>(TEnum value)
        {
            var displayAttribute = typeof(TEnum)
                .GetMember(value.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.GetName() ?? value.ToString();
        }
    }
}
