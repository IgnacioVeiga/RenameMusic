﻿using RenameMusic.Assets;
using RenameMusic.DB;
using RenameMusic.Entities;
using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Themes;
using RenameMusic.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            ThemeManager.LoadTheme();
            new MyContext().Database.EnsureCreated();

            #region Languages
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
                LanguagesMenu.Items.Add(menuItem);
            }
            #endregion Languages

            #region Themes
            foreach (string themeName in ThemeManager.Themes)
            {
                bool isThemeSelected = Settings.Default.ThemeName == themeName;
                MenuItem menuItem = new()
                {
                    Header = themeName,
                    IsCheckable = true,
                    IsChecked = isThemeSelected,
                    IsEnabled = !isThemeSelected
                };
                menuItem.Click += ThemeSelected_Click;
                ThemesMenu.Items.Add(menuItem);
            }
            #endregion Themes

            ContentLoadedSBar.Text = $"{Strings.LOADED}: 0/0";
            // ToDo: Enable the MenuItem "LoadPrevData" only if there is at least one item in the "Folders" table of the database.
            // If there is already data loaded in the DataGrid, warn the user that this data will be lost when loading data from the database.
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
            bool canRename = DAL.CountAudioItems(true) > 0;
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

            // To prevent problems
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

            // To prevent problems
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

            // To prevent problems
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
            MessageBox.Show(Strings.TASK_SUCCESSFULL_MSG);
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
                Pictures.Source = Multimedia.GetBitmapImage(pic.Data.Data);
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

        private void ReplaceWithBTN_Click(object sender, RoutedEventArgs e)
        {
            bool? Ok = new ReplaceWith().ShowDialog();
            if (Ok == true)
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
            Settings.Default.DefaultTemplate = "<TrackNum>. <Title> - <Album> (<Year>)";
            Settings.Default.TrackNumRequired = false;
            Settings.Default.TitleRequired = true;
            Settings.Default.AlbumRequired = true;
            Settings.Default.AlbumArtistRequired = false;
            Settings.Default.ArtistRequired = false;
            Settings.Default.YearRequired = false;
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

        private void ThemeSelected_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedItem = sender as MenuItem;
            string themeName = clickedItem?.Header.ToString();
            ThemeManager.ChangeTheme(themeName);

            // The selected theme must be disabled and checked.
            foreach (MenuItem themeItem in ThemesMenu.Items)
            {
                themeItem.IsEnabled = true;

                if (themeItem == clickedItem)
                {
                    themeItem.IsChecked = true;
                    themeItem.IsEnabled = false;
                }
                else
                {
                    themeItem.IsChecked = false;
                }
            }
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

        private void List_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem == null)
                ((DataGrid)sender).ContextMenu.Visibility = Visibility.Collapsed;
            else
                ((DataGrid)sender).ContextMenu.Visibility = Visibility.Visible;
        }

        private void PlayFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "";
            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    filePath = ((Audio)PrimaryList.SelectedItem).FilePath;
                    break;
                case 1:
                    filePath = ((Audio)SecondaryList.SelectedItem).FilePath;
                    break;
            }
            Process.Start(new ProcessStartInfo()
            {
                FileName = filePath,
                UseShellExecute = true // This allows you to play the file with the default audio player.
            });
        }

        private void EditTags_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "";
            // First I must know in which list the selected item is.
            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    filePath = ((Audio)PrimaryList.SelectedItem).FilePath;
                    break;
                case 1:
                    filePath = ((Audio)SecondaryList.SelectedItem).FilePath;
                    break;
            }
            MetadataEditor window = new(filePath);
            if (window.ShowDialog() == true)
            {
                MessageBox.Show(Strings.METADATA_EDIT_SUCCESS, Strings.EDIT_TAGS, MessageBoxButton.OK, MessageBoxImage.Information);

                PrimaryList.Items.Clear();
                SecondaryList.Items.Clear();
                FolderList.Items.Clear();
                LoadData();
            }
        }

        private void SwitchList_Click(object sender, RoutedEventArgs e)
        {
            int id = 0;
            // First I must know in which list the selected item is.
            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    id = ((Audio)PrimaryList.SelectedItem).Id;
                    break;
                case 1:
                    id = ((Audio)SecondaryList.SelectedItem).Id;
                    // ToDo: Check if it complies with the minimum required tags, in case it does not comply with them, notify and let it continue.
                    break;
            }
            DAL.SwitchList(id);
            PrimaryList.Items.Clear();
            SecondaryList.Items.Clear();
            FolderList.Items.Clear();
            CurrentPage = 1;
            LoadData();
        }

        private void RenameThisNow_Click(object sender, RoutedEventArgs e)
        {
            Audio audio = MainTabs.SelectedIndex switch
            {
                0 => (Audio)PrimaryList.SelectedItem,
                1 => (Audio)SecondaryList.SelectedItem,
                _ => null
            };

            string oldName = audio.FilePath;
            string newName = audio.Folder + audio.NewName + audio.Type;

            FilenameFunctions.RenameFile(oldName, newName);
            DAL.RemoveAudioFromDB(audio.Id);
            PrimaryList.Items.Clear();
            SecondaryList.Items.Clear();
            FolderList.Items.Clear();
            LoadData();
        }

        private void RemoveFromList_Click(object sender, RoutedEventArgs e)
        {
            // Perform exactly the same as clicking on the "X" button.
            // Check if you can remove this function and use the previous one mentioned above.
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            // 0. Ask before proceeding
            // 1. Block access to components
            // 2. Remove audio from DB
            // 3. Remove file from storage
            // 4. Emptying lists, reloading and unlocking components
        }

        private void DeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            // 0. Ask before proceeding
            // 1. Block access to components
            // 2. Remove folder and audios from DB
            // 3. Remove folder and files from storage
            // 4. Emptying lists, reloading and unlocking components
        }

        private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    Process.Start("explorer.exe", $"/select,\"{((Audio)PrimaryList.SelectedItem).FilePath}\"");
                    break;
                case 1:
                    Process.Start("explorer.exe", $"/select,\"{((Audio)SecondaryList.SelectedItem).FilePath}\"");
                    break;
                case 2:
                    Process.Start("explorer.exe", ((Folder)FolderList.SelectedItem).Path);
                    break;
            }
        }
    }
}
