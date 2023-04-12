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
        // ToDo: todo lo relacionado a los selectores de páginas deben de ser independientes para cada "Tab"
        public MainWindow()
        {
            InitializeComponent();
            pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
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

            PageSize = 64;
            CurrentTabIndex = 0;
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] directories = Picker.ShowFolderPicker();
            if (directories.Length == 0) return;

            foreach (string directory in directories)
            {
                string[] files = Picker.GetFilePaths(directory);
                DatabaseAPI.BeforeAddToDB(files);
            }

            CurrentTabIndex = 0;
            LoadData();
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Picker.ShowFilePicker();
            if (files.Length == 0) return;
            DatabaseAPI.BeforeAddToDB(files);
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
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
            pictures.Source = null;
            if ((Audio)((DataGrid)sender).SelectedItem is null)
            {
                pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
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

                pictures.Source = bitmap;
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

        public int CurrentTabIndex;
        public int PageSize;

        private int _currentPage;
        public int CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                LoadData();
            }
        }

        private void PrimaryList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight == e.ExtentHeight)
            {
                CurrentPage++;
            }
            else if (e.VerticalOffset == 0)
            {
                if (CurrentPage > 1)
                {
                    CurrentPage--;
                }
            }
        }
        private void SecondaryList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight == e.ExtentHeight)
            {
                CurrentPage++;
            }
            else if (e.VerticalOffset == 0)
            {
                if (CurrentPage > 1)
                {
                    CurrentPage--;
                }
            }
        }
        private void FolderList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight == e.ExtentHeight)
            {
                CurrentPage++;
            }
            else if (e.VerticalOffset == 0)
            {
                if (CurrentPage > 1)
                {
                    CurrentPage--;
                }
            }
        }

        private void PrimaryList_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
        }
        private void SecondaryList_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
        }
        private void FolderList_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
        }

        private void LoadData()
        {
            switch (CurrentTabIndex)
            {
                case 0:
                    PrimaryList.Items.Clear();
                    PrimaryList.Items.AddRange(
                        DatabaseAPI.GetPageOfAudios(PageSize, CurrentPage, true)
                        );
                    break;
                case 1:
                    SecondaryList.Items.Clear();
                    SecondaryList.Items.AddRange(
                        DatabaseAPI.GetPageOfAudios(PageSize, CurrentPage, false)
                        );
                    break;
                case 2:
                    FolderList.Items.Clear();
                    FolderList.Items.AddRange(
                        DatabaseAPI.GetPageOfFolders(PageSize, CurrentPage)
                        );
                    break;
            }
        }

        private void TabsVisibility()
        {
            tabs.Visibility = (PrimaryList.Items.Count > 0
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
            string format = Strings.PAGE + ": {0}/{1}\t" + Strings.LOADED + ": {2}/{3}";
            primaryTab.Text = string.Format(format,
                                            PageControl.PrimaryList_Page, PageControl.PrimaryListTotalPages,
                                            PrimaryList.Items.Count, DatabaseAPI.CountAudioItems(true));
            secondaryTab.Text = string.Format(format,
                                            PageControl.SecondaryList_Page, PageControl.SecondaryListTotalPages,
                                            SecondaryList.Items.Count, DatabaseAPI.CountAudioItems(false));
            folderTab.Text = string.Format(format,
                                            PageControl.FoldersList_Page, PageControl.FolderListTotalPages,
                                            FolderList.Items.Count, DatabaseAPI.CountFolderItems());
        }
    }
}
