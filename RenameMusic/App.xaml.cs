using System;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static System.Threading.Mutex _mutex = null;

        App()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("es"); 
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            string mutexId = "RenameMusic";
            _mutex = new System.Threading.Mutex(true, mutexId, out bool createdNew);
            if (createdNew)
            {
                Exit += CloseMutexHandler;
            }
            else
            {
                MessageBox.Show("No se permiten multiples instancias de este programa");
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
