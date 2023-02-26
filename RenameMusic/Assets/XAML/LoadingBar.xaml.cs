using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for LoadingBar.xaml
    /// </summary>
    public partial class LoadingBar : Window
    {
        public LoadingBar(double max)
        {
            InitializeComponent();
            loadingBarStatus.Minimum = 0;
            loadingBarStatus.Maximum = max;
        }

        public void UpdateProgress()
        {
            loadingBarStatus.Value++;
        }
    }
}
