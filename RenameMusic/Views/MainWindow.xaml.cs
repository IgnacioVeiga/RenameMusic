using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RenameMusic.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
            {
                return;
            }

            DataGridRow? row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row is null)
            {
                dataGrid.UnselectAll();
                return;
            }

            if (!row.IsSelected)
            {
                dataGrid.SelectedItem = row.Item;
            }

            row.Focus();
        }

        private static T? FindVisualParent<T>(DependencyObject? source) where T : DependencyObject
        {
            while (source is not null)
            {
                if (source is T typed)
                {
                    return typed;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }
    }
}
