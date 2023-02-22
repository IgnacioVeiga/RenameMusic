using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] folders = MyFunctions.ShowFolderPickerDialog();
            if (folders is null) return;

            try
            {
                foreach (string folderPath in folders)
                {
                    Folder folderItem = new(Guid.NewGuid().ToString("N"), folderPath + Path.DirectorySeparatorChar);

                    // Se toma lista de archivos con formato mp3, m4a y ogg de la carpeta
                    foreach (string filePath in MyFunctions.GetFilePaths(folderPath, false))
                    {
                        AudioFile audiofile = new(filePath);

                        if (audiofile.Tags is not null)
                        {
                            audiofile.Id = folderItem.Id;

                            // Según si el nuevo nombre existe, entonces al menos tiene el tag de titulo
                            if (string.IsNullOrEmpty(audiofile.NewName))
                                noTitleTagList.Items.Add(audiofile);
                            else
                                withTagsList.Items.Add(audiofile);
                        }
                    }

                    folderList.Items.Add(folderItem);
                }

                tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
                renameFilesBTN.IsEnabled = withTagsList.Items.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveList_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void RenameFilesBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Folder folderItem in folderList.Items)
                {
                    foreach (AudioFile mFileItem in withTagsList.Items)
                    {
                        if (mFileItem.Id == folderItem.Id)
                        {
                            string oldName = mFileItem.FilePath;
                            string newName = folderItem.Path + Path.DirectorySeparatorChar + mFileItem.NewName + mFileItem.Type;
                            FilenameFunctions.RenameFile(oldName, newName);
                        }
                    }
                }
                renameFilesBTN.IsEnabled = false;
                withTagsList.Items.Clear();
                noTitleTagList.Items.Clear();
                folderList.Items.Clear();
                tabs.Visibility = Visibility.Hidden;

                MessageBox.Show(Strings.TASK_SUCCESFULL_MSG);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListWithTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pictures.Source = null;
            if ((AudioFile)((ListView)sender).SelectedItem is null)
            {
                pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
                return;
            }

            if (((AudioFile)((ListView)sender).SelectedItem).Tags.Pictures.Length >= 1)
            {
                TagLib.IPicture pic = ((AudioFile)((ListView)sender).SelectedItem).Tags.Pictures[0];

                // Load you image data in MemoryStream
                MemoryStream ms = new(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                // ImageSource for System.Windows.Controls.Image
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                pictures.Source = bitmap;
            }
        }

        private void RemoveFolderItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string id = ((Folder)((Button)sender).DataContext).Id;

                for (int i = 0; i < withTagsList.Items.Count;)
                {
                    if (((AudioFile)withTagsList.Items[i]).Id.Equals(id)) withTagsList.Items.RemoveAt(i);
                    else i++;
                }

                for (int i = 0; i < noTitleTagList.Items.Count;)
                {
                    if (((AudioFile)noTitleTagList.Items[i]).Id == id) noTitleTagList.Items.RemoveAt(i);
                    else i++;
                }

                for (int i = 0; i < folderList.Items.Count;)
                {
                    if (((Folder)folderList.Items[i]).Id == id) folderList.Items.RemoveAt(i);
                    else i++;
                }

                renameFilesBTN.IsEnabled = withTagsList.Items.Count > 0;
                tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TemplateBTN_Click(object sender, RoutedEventArgs e)
        {
            Template config = new();
            config.ShowDialog();
        }

        private void RestoreSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultTemplate = "<tn>. <t> - <a>";
            Settings.Default.Save();
            MessageBox.Show(Strings.SETTINGS_RESTORED);
        }

        private void ImportSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void ExportSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void LangSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppLanguage.ChangeLanguage(((ComboBox)sender).SelectedIndex);

            if (langSelected.IsVisible)
            {
                MessageBoxResult resp = MessageBox.Show(Strings.TOGGLE_LANG_MSG, $"{Strings.RESTART}?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resp == MessageBoxResult.Yes)
                {
                    App.RestartApp();
                }
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resp = MessageBox.Show(Strings.EXIT_MSG, $"{Strings.EXIT}?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resp == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}
