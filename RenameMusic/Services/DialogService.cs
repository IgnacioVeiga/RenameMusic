using RenameMusic.Models;
using RenameMusic.Views;
using System.Windows;

namespace RenameMusic.Services
{
    public interface IDialogService
    {
        bool Confirm(string message, string title);
        void ShowInfo(string message, string title);
        void ShowWarning(string message, string title);
        void ShowError(string message, string title);
        TemplateDialogResult? ShowTemplateDialog();
        ConflictDialogResult? ShowConflictDialog(string sourcePath, string destinationPath);
        bool EditMetadata(string filePath);
    }

    public sealed class DialogService : IDialogService
    {
        public bool Confirm(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void ShowInfo(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public TemplateDialogResult? ShowTemplateDialog()
        {
            ReplaceWith dialog = new();
            return dialog.ShowDialog() == true ? dialog.Result : null;
        }

        public ConflictDialogResult? ShowConflictDialog(string sourcePath, string destinationPath)
        {
            RepeatedFile dialog = new(sourcePath, destinationPath);
            return dialog.ShowDialog() == true ? dialog.Result : null;
        }

        public bool EditMetadata(string filePath)
        {
            MetadataEditor editor = new(filePath);
            return editor.ShowDialog() == true;
        }
    }
}
