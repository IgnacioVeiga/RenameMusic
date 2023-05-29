using System.Windows;

namespace RenameMusic.Assets
{
    /// <summary>
    /// Interaction logic for LoadingBar.xaml
    /// </summary>
    public partial class LoadingBar : Window
    {
        private double value;
        private readonly double maximum;
        public LoadingBar(double max)
        {
            InitializeComponent();
            maximum = max;
        }

        public void UpdateProgress()
        {
            value++;
            loadingBarStatus.Value = (value / maximum) * 100;
        }
    }
}
