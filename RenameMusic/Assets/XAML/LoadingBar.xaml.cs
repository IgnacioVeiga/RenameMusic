using RenameMusic.Lang;
using System;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for LoadingBar.xaml
    /// </summary>
    public partial class LoadingBar : Window
    {
        public LoadingBar(double min, double max)
        {
            try
            {
                InitializeComponent();

                // 1. Recibir minimo y maximo y mapear estos valores entre 0 y 100.
                // 2. Incrementar la barra a la par del otro proceso
                progressBar.Minimum = min;
                progressBar.Maximum = max;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (progressBar.Value == progressBar.Maximum)
            {
                Close();
            }
        }

        public void Increment(double val)
        {
            progressBar.Value += val;
        }
    }
}
