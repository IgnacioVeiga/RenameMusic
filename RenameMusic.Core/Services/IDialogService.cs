using RenameMusic.Models;

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
}
