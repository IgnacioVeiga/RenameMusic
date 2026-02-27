using RenameMusic.Properties;
using RenameMusic.Models;
using System.IO;
using System.Windows;

namespace RenameMusic.Views
{
    /// <summary>
    /// Interaction logic for RepeatedFile.xaml
    /// </summary>
    public partial class RepeatedFile : Window
    {
        public ConflictDialogResult? Result { get; private set; }

        public RepeatedFile(string oldName, string newNameRepeated)
        {
            InitializeComponent();
            keepChoice.IsChecked = Settings.Default.RepeatedFileKeepChoice;
            currentName.Text = Path.GetFileName(oldName);
            newName.Text = Path.GetFileName(newNameRepeated);
            string parent = Path.GetDirectoryName(oldName) ?? string.Empty;
            location.Text = parent + Path.DirectorySeparatorChar;
        }

        private void ReplaceBTN_Click(object sender, RoutedEventArgs e)
        {
            Return(ConflictResolutionAction.Replace);
        }

        private void SkipBTN_Click(object sender, RoutedEventArgs e)
        {
            Return(ConflictResolutionAction.Skip);
        }

        private void RenameBTN_Click(object sender, RoutedEventArgs e)
        {
            Return(ConflictResolutionAction.RenameWithNumber);
        }

        private void RememberChoice_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.RepeatedFileKeepChoice = keepChoice.IsChecked == true;
            Settings.Default.Save();
        }

        private void Return(ConflictResolutionAction action)
        {
            Result = new ConflictDialogResult
            {
                Action = action,
                ApplyToAll = keepChoice.IsChecked == true
            };

            Settings.Default.RepeatedFileKeepChoice = keepChoice.IsChecked == true;
            Settings.Default.Save();
            DialogResult = true;
            Close();
        }
    }
}
