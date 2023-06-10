using RenameMusic.Lang;
using RenameMusic.Properties;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex;

        App()
        {
            SetDropDownMenuToBeRightAligned();
            AppLanguage.ChangeLanguage(Settings.Default.Lang);
        }

        internal static void RestartApp()
        {
            try
            {
                Process.Start(Environment.ProcessPath);
                Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const bool initiallyOwned = true;
            const string name = "RenameMusic";
            _mutex = new Mutex(initiallyOwned, name, out bool createdNew);
            if (createdNew)
            {
                Exit += CloseMutexHandler;
            }
            else
            {
                MessageBox.Show(Strings.MULTI_INSTANCE_MSG);
                Current.Shutdown();
            }
            base.OnStartup(e);
        }

        protected virtual void CloseMutexHandler(object sender, EventArgs e)
        {
            _mutex?.Close();
        }

        // Source: https://stackoverflow.com/a/67114984
        private static void SetDropDownMenuToBeRightAligned()
        {
            FieldInfo menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);

            static void setAlignmentValue(FieldInfo menuDropAlignmentField)
            {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            }

            setAlignmentValue(menuDropAlignmentField);

            SystemParameters.StaticPropertyChanged += (sender, e) => setAlignmentValue(menuDropAlignmentField);
        }
    }
}
