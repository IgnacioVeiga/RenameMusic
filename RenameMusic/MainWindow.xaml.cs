using RenameMusic.Entities;
using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.IO;
using System.Threading.Tasks;
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
            foreach (string directory in Picker.ShowFolderPicker())
            {
                string[] files = Picker.GetFilePaths(directory);
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

        private async void RenameFiles_Click(object sender, RoutedEventArgs e)
        {
            LoadingBar loading_bar = new(primaryList.Items.Count);
            loading_bar.Show();

            await Task.Run(() =>
            {
                foreach (Audio mFileItem in primaryList.Items)
                {
                    string oldName = mFileItem.FilePath;
                    string newName = mFileItem.Folder + mFileItem.NewName + mFileItem.Type;

                    if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) || !File.Exists(oldName))
                        continue;

                    FilenameFunctions.RenameFile(oldName, newName);
                    loading_bar.Dispatcher.Invoke(() => loading_bar.UpdateProgress());
                }
            });

            renameFilesBTN.IsEnabled = false;

            primaryList.Items.Clear();
            secondaryList.Items.Clear();
            folderList.Items.Clear();

            tabs.Visibility = Visibility.Hidden;
            loading_bar.Close();
            MessageBox.Show(Strings.TASK_SUCCESFULL_MSG);
        }

        private void ListWithTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pictures.Source = null;
            if ((Audio)((ListView)sender).SelectedItem is null)
            {
                pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
                return;
            }

            if (((Audio)((ListView)sender).SelectedItem).Tags.Pictures.Length >= 1)
            {
                TagLib.IPicture pic = ((Audio)((ListView)sender).SelectedItem).Tags.Pictures[0];

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
                    if (((Audio)primaryList.Items[i]).Id.Equals(id)) primaryList.Items.RemoveAt(i);
                    else i++;
                }

                for (int i = 0; i < secondaryList.Items.Count;)
                {
                    if (((Audio)secondaryList.Items[i]).Id == id) secondaryList.Items.RemoveAt(i);
                    else i++;
                }

                for (int i = 0; i < folderList.Items.Count;)
                {
                    if (((Folder)folderList.Items[i]).Id == id) folderList.Items.RemoveAt(i);
                    else i++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
        }

        private void TemplateBTN_Click(object sender, RoutedEventArgs e)
        {
            _ = new Template().ShowDialog();
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
            string language = (sender as MenuItem)?.Tag.ToString();
            AppLanguage.ChangeLanguage(language);
            MessageBox.Show(Strings.TOGGLE_LANG_MSG, $"{Strings.RESTARTING}", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            // ToDo: Make a temporary backup copy of the changes made to restore it later.

            App.RestartApp();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //MessageBoxResult resp = MessageBox.Show(Strings.EXIT_MSG, $"{Strings.EXIT}?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //if (resp == MessageBoxResult.Yes)
            Application.Current.Shutdown();
        }
    }
}
