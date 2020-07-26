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

        protected override void OnStartup(StartupEventArgs e)
        {
            string mutexId = "nasho-0101";
            _mutex = new System.Threading.Mutex(true, mutexId, out bool createdNew);
            if (createdNew)
            {
                Exit += CloseMutexHandler;
            }
            else
            {
                MessageBox.Show("No se permiten multiples instancias de este programa", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
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
