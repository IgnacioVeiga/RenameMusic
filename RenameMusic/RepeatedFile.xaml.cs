using RenameMusic.Lang;
using System;
using System.IO;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for RepeatedFile.xaml
    /// </summary>
    public partial class RepeatedFile : Window
    {

        public RepeatedFile(string pOldFileName, string pNewFileName)
        {
            try
            {
                InitializeComponent();
                rememberChoice.IsChecked = Properties.Settings.Default.RepeatedFileRememberChoice;

                // Nombres sin la ubicación y con formato
                currentName.Content = pOldFileName[(pOldFileName.LastIndexOf(@"\") + 1)..];
                newName.Content = pNewFileName[(pOldFileName.LastIndexOf(@"\") + 1)..];
                
                // La ubicación
                path.Content = pOldFileName.Substring(0, pOldFileName.LastIndexOf(@"\") + 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReplaceBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificar si el archivo sigue existiendo luego de esta pausa.
                if (File.Exists((string)path.Content + (string)newName.Content))
                {
                    File.Delete((string)path.Content + (string)newName.Content);
                    File.Move((string)path.Content + (string)currentName.Content,
                        (string)path.Content + (string)newName.Content);
                }
                else
                {
                    MessageBox.Show(strings.FILE_NOT_FOUND_MSG);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }

        private void SkipBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RenameBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int num = 2;
                string pathAndName = path.Content.ToString() + newName.Content.ToString()[..^4];
                string type = newName.Content.ToString().Substring(newName.Content.ToString().Length - 4);
                while (File.Exists($"{pathAndName} ({num}){type}"))
                {
                    num += 1; // se incrementa en 1 en cada ciclo
                }

                File.Move(path.Content.ToString() + currentName.Content.ToString(), $"{pathAndName} ({num}){type}");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }

        private void rememberChoice_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RepeatedFileRememberChoice = (bool)rememberChoice.IsChecked;
        }
    }
}
