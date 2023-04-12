using RenameMusic.DB;
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
using WinCopies.Util;

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
            Pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
            _ = new MyContext().Database.EnsureCreated();
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

            PageSize = 128;
            MainTabs.SelectedIndex = 0;
            LoadData();
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
        }

        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Picker.ShowFilePicker();
            if (files.Length == 0) return;

            LoadingBar loading_bar = new(files.Length);
            loading_bar.Show();

            await Task.Run(() =>
            {
                DatabaseAPI.BeforeAddToDB(files);
                loading_bar.Dispatcher.Invoke(() => loading_bar.UpdateProgress());
            });

            MainTabs.SelectedIndex = 0;
            LoadData();
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
            loading_bar.Close();
        }
        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] directories = Picker.ShowFolderPicker();
            if (directories.Length == 0) return;

            LoadingBar loading_bar = new(directories.Length);
            loading_bar.Show();

            await Task.Run(() =>
            {
                foreach (string directory in directories)
                {
                    string[] files = Picker.GetFilePaths(directory);
                    DatabaseAPI.BeforeAddToDB(files);
                    loading_bar.Dispatcher.Invoke(() => loading_bar.UpdateProgress());
                }
            });

            MainTabs.SelectedIndex = 0;
            LoadData();
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
            loading_bar.Close();
        }

        private async void RenameFiles_Click(object sender, RoutedEventArgs e)
        {
            LoadingBar loading_bar = new(DatabaseAPI.CountAudioItems(true));
            loading_bar.Show();

            await Task.Run(() =>
            {
                // ToDo: se debe renombrar todas las páginas de esa lista, no solo la cargada actualmente
                foreach (Audio mFileItem in PrimaryList.Items)
                {
                    string oldName = mFileItem.FilePath;
                    string newName = mFileItem.Folder + mFileItem.NewName + mFileItem.Type;

                    if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) || !File.Exists(oldName))
                        continue;

                    FilenameFunctions.RenameFile(oldName, newName);
                    loading_bar.Dispatcher.Invoke(() => loading_bar.UpdateProgress());
                }
            });

            TabsVisibility();
            IsEnabledRenameBTN();
            DatabaseAPI.ClearDatabase();
            UpdateTabHeader();
            loading_bar.Close();
            MessageBox.Show(Strings.TASK_SUCCESFULL_MSG);
        }

        private void AudioItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pictures.Source = null;
            if ((Audio)((DataGrid)sender).SelectedItem is null)
            {
                Pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
                return;
            }

            if (((Audio)((DataGrid)sender).SelectedItem).Tags is null) return;

            if (((Audio)((DataGrid)sender).SelectedItem).Tags.Pictures.Length >= 1)
            {
                TagLib.IPicture pic = ((Audio)((DataGrid)sender).SelectedItem).Tags.Pictures[0];

                MemoryStream ms = new(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                Pictures.Source = bitmap;
            }
        }

        private void RemoveFolderItem_Click(object sender, RoutedEventArgs e)
        {
            int folderId = ((Folder)((Button)sender).DataContext).Id;
            DatabaseAPI.RemoveFolderFromDB(folderId);
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
        }

        private void RenamingRuleBTN_Click(object sender, RoutedEventArgs e)
        {
            _ = new RenamingRule().ShowDialog();
        }

        private void RestoreSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultTemplate = "<tn>. <t> - <a>";
            Settings.Default.Save();
            MessageBox.Show(Strings.SETTINGS_RESTORED);
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

            App.RestartApp();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FolderList.Items.Count > 0)
            {
                if (MessageBox.Show(Strings.EXIT_MSG, $"{Strings.EXIT}?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    Application.Current.Shutdown();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTabHeader();
        }

        public int PageSize;
        private int _currentPage;
        public int CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                LoadData();
                UpdateTabHeader();
            }
        }

        private void PrimaryList_Loaded(object sender, RoutedEventArgs e)
        {
            PrimaryList.Items.Clear();
            CurrentPage = 1;
        }
        private void SecondaryList_Loaded(object sender, RoutedEventArgs e)
        {
            SecondaryList.Items.Clear();
            CurrentPage = 1;
        }
        private void FolderList_Loaded(object sender, RoutedEventArgs e)
        {
            FolderList.Items.Clear();
            CurrentPage = 1;
        }

        private void LoadData()
        {
            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    PrimaryList.Items.AddRange(
                        DatabaseAPI.GetPageOfAudios(PageSize, CurrentPage, true)
                        );
                    break;
                case 1:
                    SecondaryList.Items.AddRange(
                        DatabaseAPI.GetPageOfAudios(PageSize, CurrentPage, false)
                        );
                    break;
                case 2:
                    FolderList.Items.AddRange(
                        DatabaseAPI.GetPageOfFolders(PageSize, CurrentPage)
                        );
                    break;
            }
        }

        private void TabsVisibility()
        {
            MainTabs.Visibility = (PrimaryList.Items.Count > 0
                || SecondaryList.Items.Count > 0
                || FolderList.Items.Count > 0)
                ? Visibility.Visible : Visibility.Hidden;
        }
        private void IsEnabledRenameBTN()
        {
            renameFilesBTN.IsEnabled = PrimaryList.Items.Count > 0;
        }
        private void UpdateTabHeader()
        {
            string format = Strings.LOADED + ": {0}/{1}";
            PrimaryTab.Text = string.Format(format, PrimaryList.Items.Count, DatabaseAPI.CountAudioItems(true));
            SecondaryTab.Text = string.Format(format, SecondaryList.Items.Count, DatabaseAPI.CountAudioItems(false));
            FolderTab.Text = string.Format(format, FolderList.Items.Count, DatabaseAPI.CountFolderItems());
        }
        //private static int GetTotalPages(int totalItems, int pageSize)
        //{
        //    int pages = (int)Math.Ceiling((double)totalItems / pageSize);
        //    if (pages == 0) return 1;
        //    else return pages;
        //}
    }
}
