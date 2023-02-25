using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WinCopies.Linq;

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

            foreach (var language in AppLanguage.Languages)
            {
                bool isLangSelected = Settings.Default.Lang == language.Key;
                MenuItem menuItem = new()
                {
                    Tag = language.Key,
                    Header = language.Value,
                    IsCheckable = true,
                    IsChecked = isLangSelected,
                    IsEnabled = !isLangSelected
                };
                menuItem.Click += LanguageSelected_Click;
                languages.Items.Add(menuItem);
            }
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Picker.ShowFilePicker();
            if (files is null) return;

            foreach (string file in files)
            {
                Folder folder = folderList.Items.As<Folder>()
                    .FirstOrDefaultValuePredicate(f => f.Path == Path.GetDirectoryName(file) + Path.DirectorySeparatorChar);

                if (folder is null)
                {
                    folder = new(Guid.NewGuid().ToString("N"), Path.GetDirectoryName(file) + Path.DirectorySeparatorChar);
                    folderList.Items.Add(folder);
                }
                AudioFile audiofile = new(folder.Id, file);

                bool alreadyAdded =
                    withTagsList.Items.As<AudioFile>().FirstOrDefaultValuePredicate(f => f.FilePath == audiofile.FilePath) is not null
                    || noTitleTagList.Items.As<AudioFile>().FirstOrDefaultValuePredicate(f => f.FilePath == audiofile.FilePath) is not null;

                if (alreadyAdded) MessageBox.Show($"{audiofile.FilePath}", Strings.REPEATED_FILE, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                else
                {
                    if (audiofile.Tags is not null)
                    {
                        if (string.IsNullOrEmpty(audiofile.NewName))
                            noTitleTagList.Items.Add(audiofile);
                        else
                            withTagsList.Items.Add(audiofile);
                    }
                }
            }

            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            renameFilesBTN.IsEnabled = withTagsList.Items.Count > 0;

            // ToDo: Do almost the same as in the function below. See how to reuse the code without repeating it.
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] folders = Picker.ShowFolderPicker();
            if (folders is null) return;

            foreach (string folderpath in folders)
            {
                Folder folderItem = new(Guid.NewGuid().ToString("N"), folderpath + Path.DirectorySeparatorChar);
                string[] audioFilesPath = Picker.GetFilePaths(folderpath);

                if (audioFilesPath.Length > 0)
                {
                    foreach (string filepath in audioFilesPath)
                    {
                        AudioFile audiofile = new(folderItem.Id, filepath);

                        if (audiofile.Tags is not null)
                        {
                            if (string.IsNullOrEmpty(audiofile.NewName))
                                noTitleTagList.Items.Add(audiofile);
                            else
                                withTagsList.Items.Add(audiofile);
                        }
                    }
                    folderList.Items.Add(folderItem);
                }
            }

            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            renameFilesBTN.IsEnabled = withTagsList.Items.Count > 0;
        }

        private void SaveList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
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
            MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void ExportSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void IncludeSubFolders_Check(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }

        private void LanguageSelected_Click(object sender, RoutedEventArgs e)
        {
            string lang = (sender as MenuItem)?.Tag.ToString();
            AppLanguage.ChangeLanguage(lang);
            MessageBox.Show(Strings.TOGGLE_LANG_MSG, $"{Strings.RESTARTING}", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            // ToDo: Make a temporary backup copy of the changes made to restore it later.

            App.RestartApp();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resp = MessageBox.Show(Strings.EXIT_MSG, $"{Strings.EXIT}?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resp == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}
