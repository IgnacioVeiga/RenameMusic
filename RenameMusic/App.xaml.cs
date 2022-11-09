using System;
using System.Windows;
using RenameMusic.Lang;
using RenameMusic.Properties;
using System.Threading;
using System.Globalization;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;

        App()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.lang);

            if (string.IsNullOrWhiteSpace(Settings.Default.DefaultTemplate))
            {
                Settings.Default.DefaultTemplate = "<tn>. <t> - <a>";
                Settings.Default.Save();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string name = "RenameMusic";
            const bool initiallyOwned = true;
            _mutex = new Mutex(initiallyOwned, name, out bool createdNew);
            if (createdNew)
            {
                Exit += CloseMutexHandler;
            }
            else
            {
                MessageBox.Show(strings.MULTI_INSTANCE_MSG);
                Current.Shutdown();
            }
            base.OnStartup(e);
        }

        protected virtual void CloseMutexHandler(object sender, EventArgs e)
        {
            _mutex?.Close();
        }
    }
}
