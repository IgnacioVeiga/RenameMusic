using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System;
using System.IO;
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
            pictures.Source = new BitmapImage(new Uri("./Assets/Icons/icon.ico", UriKind.Relative));

            /*
             * TODO: leer base de datos y completar las tablas,
             * validar todos los items de cada pestaña que aún existen
             * (En caso de que no existan se deben resaltar en color rojo)
             */
        }

        private void AddFolderBTN_Click(object sender, RoutedEventArgs e)
        {
            string[] folders = MyFunctions.ShowFolderPickerDialog();
            if (folders is null) return;

            try
            {
                foreach (string folderPath in folders)
                {
                    Folder folderItem = new(Guid.NewGuid().ToString("N"), folderPath + Path.DirectorySeparatorChar);

                    // Se toma lista de archivos con formato mp3, m4a y ogg de la carpeta
                    foreach (string filePath in MyFunctions.GetFilePaths(folderPath, false))
                    {
                        AudioFile audiofile = new(filePath);

                        if (audiofile.Tags is not null)
                        {
                            audiofile.Id = folderItem.Id;

                            // Según si el nuevo nombre existe, entonces al menos tiene el tag de titulo
                            if (string.IsNullOrEmpty(audiofile.NewName))
                                noTitleTagList.Items.Add(audiofile);
                            else
                                withTagsList.Items.Add(audiofile);
                        }
                    }

                    folderList.Items.Add(folderItem);
                }
                renameFilesBTN.IsEnabled = withTagsList.Items.Count > 0;
            }

            // TODO: generar un archivo de log con todos los errores y encriptarlo
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameFilesBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reviso la tabla de carpetas
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
                // Obtengo el id de los archivos asociados a la carpeta seleccionada
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TemplateBTN_Click(object sender, RoutedEventArgs e)
        {
            ConfigTemplate config = new();
            config.ShowDialog();
        }

        private void RestoreSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultTemplate = "<tn>. <t> - <a>";
            Settings.Default.Save();
            MessageBox.Show(Strings.SETTINGS_RESTORED_MSG);
        }

        private void ImportSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void ExportSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(Strings.NOT_IMPLEMENTED_MSG);
        }

        private void LangSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppLanguage.ChangeLanguage(((ComboBox)sender).SelectedIndex);

            if (langSelected.IsVisible)
            {
                MessageBoxResult resp = MessageBox.Show(Strings.TOGGLE_LANG_MSG, "REINICIAR AHORA?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resp == MessageBoxResult.Yes)
                {
                    App.RestartApp();
                }
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resp = MessageBox.Show("DESEA SALIR DEL PROGRAMA?","SALIR", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resp == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}
