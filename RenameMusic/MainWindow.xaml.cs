using RenameMusic.DB;
using RenameMusic.Entities;
using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.Collections.Generic;
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

            for (int pageSizeItem = 5; pageSizeItem <= 1280;)
            {
                PageSizeBox.Items.Add(pageSizeItem);
                pageSizeItem *= 2;
            }

            TabsVisibility();
            IsEnabledRenameBTN();

            // La base de datos se lee sola cuando ejecutamos esto:
            PageSizeBox.SelectedIndex = 0;
            PageBox.SelectedIndex = 0;
            UpdateTabHeader();
            // ToDo: revisar si las lineas anteriores son necesarias para evitar la redundancia
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string[] directories = Picker.ShowFolderPicker();
            if (directories.Length == 0) return;

            foreach (string directory in directories)
            {
                string[] files = Picker.GetFilePaths(directory);
                DatabaseAPI.AddToDatabase(files);
            }
            PageBox.IsEnabled = TotalPages > 0;
            PageBox.ItemsSource = Enumerable.Range(1, TotalPages);
            PageBox.SelectedIndex = 0;
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;

            ClearTabLists();
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Picker.ShowFilePicker();
            if (files.Length == 0) return;
            DatabaseAPI.AddToDatabase(files);

            PageBox.IsEnabled = TotalPages > 0;
            PageBox.ItemsSource = Enumerable.Range(1, TotalPages);
            PageBox.SelectedIndex = 0;
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;

            ClearTabLists();
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
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

            PageBox.ItemsSource = Enumerable.Range(1, 1);
            PageBox.IsEnabled = false;
            PageLeft.IsEnabled = false;
            PageRight.IsEnabled = false;
            ClearTabLists();
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
            if ((Audio)((ListView)sender).SelectedItem is null)
            {
                pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));
                pictures.Opacity = 0.5;
                return;
            }

            if (((Audio)((ListView)sender).SelectedItem).Tags is null) return;

            if (((Audio)((ListView)sender).SelectedItem).Tags.Pictures.Length >= 1)
            {
                TagLib.IPicture pic = ((Audio)((ListView)sender).SelectedItem).Tags.Pictures[0];

                MemoryStream ms = new(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                pictures.Source = bitmap;
                pictures.Opacity = 1;
            }
        }

        private void RemoveFolderItem_Click(object sender, RoutedEventArgs e)
        {
            int folderId = ((Folder)((Button)sender).DataContext).Id;
            DatabaseAPI.RemoveFolderFromDB(folderId);

            ClearTabLists();
            page = 1;
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);

            PageBox.IsEnabled = TotalPages > 0;
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
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
            // Si hay items aún, preguntar antes de salir
            if (folderList.Items.Count > 0)
            {
                if (MessageBox.Show(Strings.EXIT_MSG, $"{Strings.EXIT}?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    Application.Current.Shutdown();
            }
            else Application.Current.Shutdown();
        }

        private void ChangePage(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Content)
            {
                case ">":
                    page++;
                    break;

                case "<":
                    page--;
                    break;

                default:
                    return;
            }
            Page.Text = $"Page {page}";
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;
            ClearTabLists();
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);
            TabsVisibility();
            IsEnabledRenameBTN();
            PageBox.SelectedValue = page;
            UpdateTabHeader();
        }

        private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            page = (int)PageBox.SelectedValue;
            Page.Text = $"Page {page}";
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;
            ClearTabLists();
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
        }

        private void PageSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            page = 1;
            Page.Text = $"Page {page}";
            PageLeft.IsEnabled = page > 1;
            if (PageBox != null)
            {
                PageBox.IsEnabled = TotalPages > 0;
                PageBox.SelectedIndex = 0;
                PageBox.ItemsSource = Enumerable.Range(1, TotalPages);
                PageRight.IsEnabled = page < PageBox.Items.Count;
            }
            ClearTabLists();
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);
            TabsVisibility();
            IsEnabledRenameBTN();
            UpdateTabHeader();
        }

        private void PageBox_DropDownOpened(object sender, EventArgs e)
        {
            PageBox.ItemsSource = Enumerable.Range(1, TotalPages);
        }

        #region mover
        private static int page = 1;
        private int TotalPages
        {
            get
            {
                int totalItems = (int)Math.Ceiling((double)(
                    DatabaseAPI.CountAudioItems() / (int)PageSizeBox.SelectedItem
                    ));
                if (totalItems == 0) return 1;
                else return totalItems;
            }
        }

        private void TabsVisibility()
        {
            tabs.Visibility = (primaryList.Items.Count > 0) ? Visibility.Visible : Visibility.Hidden;
        }
        private void IsEnabledRenameBTN()
        {
            renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
        }
        private void FromDatabaseToListView(int pageSize, int pageNumber)
        {
            // Desde la base de datos se debe retornar una lista pequeña para cada una de las 3 listas.
            // Esto último debe funcionar como las páginas de un libro, con la posibilidad de elegir la
            // cantidad de elementos a mostrar por cada página.
            List<AudioDTO> audios = new();
            List<FolderDTO> folders = new();

            using (MyContext context = new())
            {
                audios = context.Audios
                    .OrderBy(p => p.Id) // ordena los elementos para asegurarse de obtener el rango correcto
                    .Skip((pageNumber - 1) * pageSize) // salta los primeros elementos del rango
                    .Take(pageSize) // selecciona los siguientes elementos
                    .ToList(); // convierte los elementos seleccionados en una lista

                folders = context.Folders
                    .OrderBy(p => p.Id).Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize).ToList();
            }

            // Mapear AudioDTO a la clase Audio y retornar la lista
            foreach (AudioDTO audio in audios)
            {
                Audio item = new(audio.FolderId, new MyContext().Folders.First(f => f.Id == audio.FolderId).FolderPath + audio.FileName);

                if (item.Tags != null)
                {
                    if (string.IsNullOrWhiteSpace(item.Tags.Title))
                        secondaryList.Items.Add(item);
                    else
                        primaryList.Items.Add(item);
                }
                //else
                //{
                //    // ToDo: enseñar un mensaje con los archivos corruptos
                //}
            }

            // Mapear FolderDTO a la clase Folder y retornar la lista
            foreach (FolderDTO folder in folders)
            {
                folderList.Items.Add(new Folder(folder.Id, folder.FolderPath));
            }
        }
        private void ClearTabLists()
        {
            primaryList.Items.Clear();
            secondaryList.Items.Clear();
            folderList.Items.Clear();
        }
        private void UpdateTabHeader()
        {
            // ToDo: traducir esa parte del Header
            const string format = "Page: {0}/{1}\tLoaded: {2}/{3}";
            primaryTab.Text =   string.Format(format,   page, TotalPages, primaryList.Items.Count,      DatabaseAPI.CountAudioItems());
            secondaryTab.Text = string.Format(format,   page, TotalPages, secondaryList.Items.Count,    DatabaseAPI.CountAudioItems());
            folderTab.Text =    string.Format(format,   page, TotalPages, folderList.Items.Count,       DatabaseAPI.CountFolderItems());
        }
        #endregion mover
    }
}
