using RenameMusic.DB;
using RenameMusic.Entities;
using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.Collections.Generic;
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
            new MyContext().Database.EnsureCreated();

            #region Lang
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
                Languages.Items.Add(menuItem);
            }
            #endregion Lang

            ContentLoadedSBar.Text = $"{Strings.LOADED}: 0/0";
        }

        #region Util
        public int CurrentPage;

        private void TabsVisibility()
        {
            MainTabs.Visibility = (PrimaryList.Items.Count > 0
                || SecondaryList.Items.Count > 0
                || FolderList.Items.Count > 0)
                ? Visibility.Visible : Visibility.Hidden;
        }
        private void CheckRenameBTN()
        {
            bool canRename = PrimaryList.Items.Count > 0;
            RenameMenuItemBTN.IsEnabled = canRename;
            RenameBTN.IsEnabled = canRename;
        }
        private void LoadData()
        {
            string format = Strings.LOADED + ": {0}/{1}";
            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    PrimaryList.Items.AddRange(
                        DAL.GetPageOfAudios(Settings.Default.PageSize, CurrentPage, true)
                        );

                    ContentLoadedSBar.Text = string.Format(format,
                        PrimaryList.Items.Count, DAL.CountAudioItems(true));
                    break;
                case 1:
                    SecondaryList.Items.AddRange(
                        DAL.GetPageOfAudios(Settings.Default.PageSize, CurrentPage, false)
                        );

                    ContentLoadedSBar.Text = string.Format(format,
                        SecondaryList.Items.Count, DAL.CountAudioItems(false));
                    break;
                case 2:
                    FolderList.Items.AddRange(
                        DAL.GetPageOfFolders(Settings.Default.PageSize, CurrentPage)
                        );

                    ContentLoadedSBar.Text = string.Format(format,
                        FolderList.Items.Count, DAL.CountFolderItems());
                    break;
            }
        }
        #endregion Util

        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Picker.ShowFilePicker();
            if (files.Length == 0) return;

            LoadingBar loading_bar = new(files.Length);
            loading_bar.Show();

            // Para prevenir problemas
            RenameBTN.IsEnabled = false;
            MainTabs.IsEnabled = false;
            MainMenu.IsEnabled = false;

            await Task.Run(() =>
            {
                DAL.BeforeAddToDB(files);
                loading_bar.Dispatcher.Invoke(() => loading_bar.UpdateProgress());
            });

            MainTabs.SelectedIndex = 0;
            MainStatusBar.Text = Strings.READY;
            LoadData();
            TabsVisibility();
            CheckRenameBTN();
            RenameBTN.IsEnabled = true;
            MainTabs.IsEnabled = true;
            MainMenu.IsEnabled = true;
            loading_bar.Close();
        }
        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] directories = Picker.ShowFolderPicker();
            if (directories.Length == 0) return;

            LoadingBar loading_bar = new(directories.Length);
            loading_bar.Show();

            // Para prevenir problemas
            RenameBTN.IsEnabled = false;
            MainTabs.IsEnabled = false;
            MainMenu.IsEnabled = false;

            await Task.Run(() =>
            {
                foreach (string directory in directories)
                {
                    string[] files = Picker.GetFilePaths(directory);
                    DAL.BeforeAddToDB(files);
                    loading_bar.Dispatcher.Invoke(() => loading_bar.UpdateProgress());
                    Dispatcher.Invoke(() => MainStatusBar.Text = directory);
                }
            });

            MainTabs.SelectedIndex = 0;
            MainStatusBar.Text = Strings.READY;
            LoadData();
            TabsVisibility();
            CheckRenameBTN();
            RenameBTN.IsEnabled = true;
            MainTabs.IsEnabled = true;
            MainMenu.IsEnabled = true;
            loading_bar.Close();
        }

        private void LoadPrevData_Click(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedIndex = 0;
            PrimaryList.Items.Clear();
            SecondaryList.Items.Clear();
            FolderList.Items.Clear();
            CurrentPage = 1;
            LoadData();
            TabsVisibility();
            CheckRenameBTN();
        }

        private async void RenameFiles_Click(object sender, RoutedEventArgs e)
        {
            int totalItems = DAL.CountAudioItems(true);
            LoadingBar loading_bar = new(totalItems);
            loading_bar.Show();

            CurrentPage = 1;

            // Para prevenir problemas
            RenameBTN.IsEnabled = false;
            MainTabs.IsEnabled = false;
            MainMenu.IsEnabled = false;

            await Task.Run(() =>
            {
                while (CurrentPage <= totalItems)
                {
                    string oldName = "", newName = "";
                    List<Audio> PartialAudioList = DAL.GetPageOfAudios(Settings.Default.PageSize, CurrentPage, true);
                    foreach (Audio audioItem in PartialAudioList)
                    {
                        oldName = audioItem.FilePath;
                        newName = audioItem.Folder + audioItem.NewName + audioItem.Type;

                        loading_bar.Dispatcher.Invoke(() => loading_bar.UpdateProgress());
                        Dispatcher.Invoke(() => MainStatusBar.Text = $"[{oldName}] -> [{newName}]");

                        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) || !File.Exists(oldName))
                            continue;

                        FilenameFunctions.RenameFile(oldName, newName);
                    }
                    PartialAudioList.Clear();
                    CurrentPage++;
                }
            });

            CurrentPage = 1;
            MainStatusBar.Text = Strings.READY;
            ContentLoadedSBar.Text = $"{Strings.LOADED}: 0/0";
            MainTabs.SelectedIndex = 0;

            PrimaryList.Items.Clear();
            SecondaryList.Items.Clear();
            FolderList.Items.Clear();

            MainTabs.IsEnabled = true;
            MainMenu.IsEnabled = true;

            TabsVisibility();
            CheckRenameBTN();
            DAL.ClearDatabase();

            loading_bar.Close();
            MessageBox.Show(Strings.TASK_SUCCESFULL_MSG);
        }

        private void AudioItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pictures.Source = null;
            if ((Audio)((DataGrid)sender).SelectedItem is null)
            {
                MainStatusBar.Text = Strings.READY;
                return;
            }
            MainStatusBar.Text = ((Audio)((DataGrid)sender).SelectedItem).FilePath;

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
            DAL.RemoveFolderFromDB(folderId);

            PrimaryList.Items.Clear();
            SecondaryList.Items.Clear();
            FolderList.Items.Clear();
            CurrentPage = 1;

            LoadData();
            TabsVisibility();
            CheckRenameBTN();
        }

        private void RenamingRuleBTN_Click(object sender, RoutedEventArgs e)
        {
            bool? ruleChanged = new RenamingRule().ShowDialog();
            if (ruleChanged == true)
            {
                PrimaryList.Items.Clear();
                SecondaryList.Items.Clear();
                FolderList.Items.Clear();
                LoadData();
            }
        }

        private void RestoreSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Lang = "en";
            Settings.Default.DefaultTemplate = "<tn>. <t> - <a>";
            Settings.Default.TitleRequired = true;
            Settings.Default.AlbumRequired = true;
            Settings.Default.AlbumArtistRequired = false;
            Settings.Default.ArtistRequired = false;
            Settings.Default.IncludeSubFolders = true;
            Settings.Default.RepeatedFileKeepChoice = false;
            Settings.Default.Save();
            MessageBox.Show(Strings.SETTINGS_RESTORED);
            App.RestartApp();
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

        private void PrimaryList_Loaded(object sender, RoutedEventArgs e)
        {
            if (PrimaryList.IsVisible)
            {
                PrimaryList.Items.Clear();
                CurrentPage = 1;
                LoadData();
            }
        }
        private void SecondaryList_Loaded(object sender, RoutedEventArgs e)
        {
            if (SecondaryList.IsVisible)
            {
                SecondaryList.Items.Clear();
                CurrentPage = 1;
                LoadData();
            }
        }
        private void FolderList_Loaded(object sender, RoutedEventArgs e)
        {
            if (FolderList.IsVisible)
            {
                FolderList.Items.Clear();
                CurrentPage = 1;
                LoadData();
            }
        }
    }
}
