using RenameMusic.DB;
using RenameMusic.Entities;
using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
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

            // ToDo: configurar selector de páginas y selector de cantidad de items
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] directories = Picker.ShowFolderPicker();
            if (directories.Length == 0) return;
            LoadingBar loadingBar = new(directories.Length);
            loadingBar.Show();

            foreach (string directory in directories)
            {
                await Task.Run(() =>
                {
                    string[] files = Picker.GetFilePaths(directory);
                    ListManager.AddToDatabase(files);
                    loadingBar.Dispatcher.Invoke(() => loadingBar.UpdateProgress());
                });
            }

            // ToDo: Actualizar las listas con solo algunos pocos elementos

            loadingBar.Close();
            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
            PageBox.IsEnabled = TotalPages > 0;
            PageBox.SelectedIndex = 0;
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Picker.ShowFilePicker();
            if (files.Length == 0) return;
            ListManager.AddToDatabase(files);

            // ToDo: Actualizar las listas con solo algunos pocos elementos

            tabs.Visibility = (folderList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
            renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
            PageBox.IsEnabled = TotalPages > 0;
            PageBox.SelectedIndex = 0;
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
            PageBox.IsEnabled = false;

            primaryList.Items.Clear();
            secondaryList.Items.Clear();
            folderList.Items.Clear();

            tabs.Visibility = Visibility.Hidden;
            loading_bar.Close();
            MessageBox.Show(Strings.TASK_SUCCESFULL_MSG);
        }

        private void AudioItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pictures.Source = null;
            if ((Audio)((ListView)sender).SelectedItem is null)
            {
                pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
                return;
            }

            if (((Audio)((ListView)sender).SelectedItem).Tags is null) return;

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
            int folderId = ((Folder)((Button)sender).DataContext).Id;
            ListManager.RemoveFolderFromDB(folderId);
            // ToDo: refrescar listas

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

        private static int page = 1;
        private static int TotalPages {
            get
            {
                int totalItems = new MyContext().Audios.Count();
                return (int)Math.Ceiling((double)(totalItems / 20));
            }
        }
        private void DecrementPage(object sender, RoutedEventArgs e)
        {
            --page;
            Page.Text = $"Page {page}";
        }
        private void IncrementPage(object sender, RoutedEventArgs e)
        {
            ++page;
            Page.Text = $"Page {page}";
        }

        private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            page = (int)PageBox.SelectedValue;
            Page.Text = $"Page {page}";
            // ToDo: implementar
        }

        private void PerPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            page = 1;
            Page.Text = $"Page {page}";
            // ToDo: implementar
        }

        private void PageBox_DropDownOpened(object sender, EventArgs e)
        {
            PageBox.ItemsSource = Enumerable.Range(1, TotalPages);
        }
    }
}
