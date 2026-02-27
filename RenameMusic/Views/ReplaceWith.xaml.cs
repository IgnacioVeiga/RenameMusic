using RenameMusic.Models;
using RenameMusic.ViewModels;
using System.Windows;

namespace RenameMusic.Views
{
    /// <summary>
    /// Interaction logic for ReplaceWith.xaml
    /// </summary>
    public partial class ReplaceWith : Window
    {
        private readonly ReplaceWithViewModel _viewModel;

        public TemplateDialogResult? Result => _viewModel.Result;

        public ReplaceWith()
        {
            InitializeComponent();
            _viewModel = new ReplaceWithViewModel();
            _viewModel.CloseRequested += HandleCloseRequested;
            DataContext = _viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.CloseRequested -= HandleCloseRequested;
            base.OnClosed(e);
        }

        private void HandleCloseRequested(bool? dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }
    }
}
