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
            InitializeComponent();
            loadingBarStatus.Minimum = min;
            loadingBarStatus.Maximum = max;
        }

        private void loadingBarStatus_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (loadingBarStatus.Value == loadingBarStatus.Maximum)
            {
                Close();
            }
        }

        public void Increment(double val)
        {
            loadingBarStatus.Value += val;
        }
    }
}
