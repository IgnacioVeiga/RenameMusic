using RenameMusic.Lang;
using RenameMusic.Properties;
using System;
using System.IO;
using System.Windows;

namespace RenameMusic.Assets
{
    /// <summary>
    /// Interaction logic for RepeatedFile.xaml
    /// </summary>
    public partial class RepeatedFile : Window
    {
        public RepeatedFile(string oldName, string newName_Repeated)
        {
            InitializeComponent();
            keepChoice.IsChecked = Settings.Default.RepeatedFileKeepChoice;
            currentName.Text = Path.GetFileName(oldName);
            newName.Text = Path.GetFileName(newName_Repeated);
            location.Text = Path.GetDirectoryName(oldName) + Path.DirectorySeparatorChar;
        }

        private void ReplaceBTN_Click(object sender, RoutedEventArgs e)
        {
            string oldFile = location.Text + currentName.Text;
            string newFile = location.Text + newName.Text;
            try
            {
                if (File.Exists(newFile))
                    File.Delete(newFile);

                File.Move(oldFile, newFile);
            }
            catch (Exception)
            {
                MessageBox.Show(oldFile, Strings.FILE_NOT_FOUND_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }

        private void SkipBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RenameBTN_Click(object sender, RoutedEventArgs e)
        {
            int num = 2;
            string newFile = location.Text + newName.Text;
            string dirAndFileName = location.Text + Path.GetFileNameWithoutExtension(newFile);
            string extension = Path.GetExtension(newFile);
            while (File.Exists($"{dirAndFileName} ({num}){extension}"))
            {
                num++;
            }

            try
            {
                File.Move(location.Text + currentName.Text, $"{dirAndFileName} ({num}){extension}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }

        private void RememberChoice_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.RepeatedFileKeepChoice = (bool)keepChoice.IsChecked;
            Settings.Default.Save();
        }
    }
}
