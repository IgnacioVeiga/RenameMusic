using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.IO;
using System.Threading.Tasks;
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

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            foreach (string folderpath in Picker.ShowFolderPicker())
            {
                string[] files = Picker.GetFilePaths(folderpath);
                ListManager.AddFilesToListView(files, primaryList, secondaryList, folderList);
            }

            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            ListManager.AddFilesToListView(Picker.ShowFilePicker(), primaryList, secondaryList, folderList);

            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
        }

        private void SaveList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private async void RenameFilesBTN_Click(object sender, RoutedEventArgs e)
        {
            LoadingBar loadingbar = new(primaryList.Items.Count);
            loadingbar.Show();

            await Task.Run(() =>
            {
                foreach (AudioFile mFileItem in primaryList.Items)
                {
                    string oldName = mFileItem.FilePath;
                    string newName = mFileItem.Folder + mFileItem.NewName + mFileItem.Type;

                    if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) || !File.Exists(oldName))
                        continue;

                    FilenameFunctions.RenameFile(oldName, newName);
                    loadingbar.Dispatcher.Invoke(() => loadingbar.UpdateProgress());
                }
            });

            renameFilesBTN.IsEnabled = false;
            primaryList.Items.Clear();
            secondaryList.Items.Clear();
            folderList.Items.Clear();
            tabs.Visibility = Visibility.Hidden;
            loadingbar.Close();
            MessageBox.Show(Strings.TASK_SUCCESFULL_MSG);
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

                for (int i = 0; i < primaryList.Items.Count;)
                {
                    if (((AudioFile)primaryList.Items[i]).Id.Equals(id)) primaryList.Items.RemoveAt(i);
                    else i++;
                }

                for (int i = 0; i < secondaryList.Items.Count;)
                {
                    if (((AudioFile)secondaryList.Items[i]).Id == id) secondaryList.Items.RemoveAt(i);
                    else i++;
                }

                for (int i = 0; i < folderList.Items.Count;)
                {
                    if (((Folder)folderList.Items[i]).Id == id) folderList.Items.RemoveAt(i);
                    else i++;
                }

                renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
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

        // ToDo: mover a otro lado
        void AddFilesToFolderLists(string[] files)
        {
            foreach (string file in files)
            {
                bool alreadyAdded = primaryList.Items.As<AudioFile>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null
                                 || secondaryList.Items.As<AudioFile>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null;

                if (alreadyAdded)
                {
                    continue;
                }

                string folderPath = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar;
                Folder folder = folderList.Items.As<Folder>().FirstOrDefaultValuePredicate(f => f.Path == folderPath);
                if (folder is null)
                {
                    string id = Guid.NewGuid().ToString("N");
                    folder = new(id, folderPath);
                    folderList.Items.Add(folder);
                }

                AudioFile audiofile = new(folder.Id, file);

                if (audiofile.Tags is not null)
                {
                    if (string.IsNullOrEmpty(audiofile.NewName))
                        secondaryList.Items.Add(audiofile);
                    else
                        primaryList.Items.Add(audiofile);
                }
            }
        }

    }
}
