using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for LoadingN39.xaml
    /// </summary>
    public partial class LoadingN39 : Window
    {
        public LoadingN39(double min, double max)
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
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
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
