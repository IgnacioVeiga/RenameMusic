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
        // ToDo: llamar estás funciones donde sea necesario
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
                Audio item = new(audio.FolderId, audio.FilePath);

                if (item.Tags != null)
                {
                    if(string.IsNullOrWhiteSpace(item.Tags.Title))
                        secondaryList.Items.Add(item);
                    else
                        primaryList.Items.Add(item);
                }
                else
                {
                    // ToDo: enseñar un mensaje con los archivos corruptos
                }
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

        public MainWindow()
        {
            InitializeComponent();

            // Crea la base de datos si no existe
            _ = new MyContext().Database.EnsureCreated();
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

            for (int pageSizeItem = 5; pageSizeItem <= 1280;)
            {
                PageSizeBox.Items.Add(pageSizeItem);
                pageSizeItem *= 2;
            }

            TabsVisibility();
            IsEnabledRenameBTN();
            PageSizeBox.SelectedIndex = 0;
            // ToDo: configurar selector de páginas y selector de cantidad de items
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
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            string[] files = Picker.ShowFilePicker();
            if (files.Length == 0) return;
            DatabaseAPI.AddToDatabase(files);

            // ToDo: Actualizar las listas con solo algunos pocos elementos

            PageBox.IsEnabled = TotalPages > 0;
            PageBox.ItemsSource = Enumerable.Range(1, TotalPages);
            PageBox.SelectedIndex = 0;
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;
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

            PageBox.Items.Clear();
            PageBox.IsEnabled = false;
            PageLeft.IsEnabled = false;
            PageRight.IsEnabled = false;
            ClearTabLists();
            TabsVisibility();
            IsEnabledRenameBTN();
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
            DatabaseAPI.RemoveFolderFromDB(folderId);
            // ToDo: refrescar listas

            PageBox.IsEnabled = TotalPages > 0;
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;
            renameFilesBTN.IsEnabled = primaryList.Items.Count > 0;
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
        private int TotalPages
        {
            get
            {
                int totalItems = new MyContext().Audios.Count();
                return (int)Math.Ceiling((double)(totalItems / (int)PageSizeBox.SelectedItem));
            }
        }
        private void DecrementPage(object sender, RoutedEventArgs e)
        {
            page--;
            Page.Text = $"Page {page}";
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;
            ClearTabLists();
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);
            TabsVisibility();
            IsEnabledRenameBTN();
            PageBox.SelectedValue = page;
        }
        private void IncrementPage(object sender, RoutedEventArgs e)
        {
            page++;
            Page.Text = $"Page {page}";
            PageLeft.IsEnabled = page > 1;
            PageRight.IsEnabled = page < PageBox.Items.Count;
            ClearTabLists();
            FromDatabaseToListView((int)PageSizeBox.SelectedValue, page);
            TabsVisibility();
            IsEnabledRenameBTN();
            PageBox.SelectedValue = page;
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
            // ToDo: implementar
        }

        private void PageSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            page = 1;
            Page.Text = $"Page {page}";
            PageLeft.IsEnabled = page > 1;
            if (PageBox != null)
            {
                PageRight.IsEnabled = page < PageBox.Items.Count;
            }
            // ToDo: implementar
        }

        private void PageBox_DropDownOpened(object sender, EventArgs e)
        {
            PageBox.ItemsSource = Enumerable.Range(1, TotalPages);
        }
    }
}
