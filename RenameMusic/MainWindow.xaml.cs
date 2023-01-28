using RenameMusic.Lang;
using RenameMusic.Properties;
using System;
using System.Collections.Generic;
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
            try
            {
                InitializeComponent();
                pictures.Source = new BitmapImage(new Uri("./icon.ico", UriKind.Relative));

                /*
                 * TODO: leer base de datos y completar las tablas,
                 * validar todos los items de cada pestaña que aún existen
                 * (En caso de que no existan se deben resaltar en color rojo)
                 */
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                // TODO: generar un log en cada exception y cambiar el mensaje de error por uno más amigable
            }
        }

        private void AddFolderBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Seleccionamos carpetas
                List<string> selectedFolders = MyFunctions.SelectAndListFolders();
                if (selectedFolders is null) return;

                // Ahora hay que tomar de los directorios el contenido que buscamos
                // Esto puede tardar mucho, hay que hacerlo en segundo plano
                foreach (string folderPath in selectedFolders)
                {
                    MusicFolder folderItem = new()
                    {
                        CancionesId = Guid.NewGuid().ToString("N"),
                        Ruta = folderPath
                    };

                    // Se toma lista de archivos con formato mp3, m4a y ogg de la carpeta
                    foreach (string filePath in MyFunctions.GetFilePaths(folderPath, false))
                    {
                        // Creo el objeto con la libreria TagLib en un metodo aparte para capturar errores
                        MusicFile mFile = MyFunctions.GetMusicFile(filePath);

                        if (mFile is not null)
                        {
                            // Se le asocia el ID de su carpeta
                            mFile.CarpetaId = folderItem.CancionesId;

                            // Según si el nuevo nombre existe, entonces al menos tiene el tag de titulo
                            if (string.IsNullOrEmpty(mFile.NuevoNombre))
                                noTitleTagList.Items.Add(mFile);
                            else
                                withTagsList.Items.Add(mFile);
                        }
                    }

                    folderList.Items.Add(folderItem);
                }
                renameFilesBTN.IsEnabled = withTagsList.Items.Count > 0;
            }

            // TODO: generar un archivo de log con todos los errores y encriptarlo
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameFilesBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reviso la tabla de carpetas
                foreach (MusicFolder folderItem in folderList.Items)
                {
                    foreach (MusicFile mFileItem in withTagsList.Items)
                    {
                        // Solo se trabaja con los items con los "checkboxes" marcados
                        if (mFileItem.Activo && mFileItem.CarpetaId == folderItem.CancionesId)
                        {
                            string oldFileName = folderItem.Ruta + @"\" + mFileItem.NombreActual + "." + mFileItem.Formato;
                            string newFileName = folderItem.Ruta + @"\" + mFileItem.NuevoNombre + "." + mFileItem.Formato;
                            MyFunctions.RenameFile(oldFileName, newFileName);
                        }
                    }
                }
                renameFilesBTN.IsEnabled = false;
                withTagsList.Items.Clear();
                noTitleTagList.Items.Clear();
                folderList.Items.Clear();

                MessageBox.Show(strings.TASK_SUCCESFULL_MSG);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListWithTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pictures.Source = null;
            if ((MusicFile)((ListView)sender).SelectedItem is null)
            {
                pictures.Source = new BitmapImage(new Uri("./icon.ico", UriKind.Relative));
                return;
            }

            if (((MusicFile)((ListView)sender).SelectedItem).Pictures.Length >= 1)
            {
                TagLib.IPicture pic = ((MusicFile)((ListView)sender).SelectedItem).Pictures[0];

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
                string id = ((MusicFolder)((Button)sender).DataContext).CancionesId;

                for (int i = 0; i < withTagsList.Items.Count;)
                {
                    if (((MusicFile)withTagsList.Items[i]).CarpetaId.Equals(id))
                    {
                        withTagsList.Items.RemoveAt(i);
                    }
                    else i++;
                }

                for (int i = 0; i < noTitleTagList.Items.Count;)
                {
                    if (((MusicFile)noTitleTagList.Items[i]).CarpetaId == id)
                    {
                        noTitleTagList.Items.RemoveAt(i);
                    }
                    else i++;
                }

                for (int i = 0; i < folderList.Items.Count;)
                {
                    if (((MusicFolder)folderList.Items[i]).CancionesId == id)
                    {
                        folderList.Items.RemoveAt(i);
                    }
                    else i++;
                }

                renameFilesBTN.IsEnabled = withTagsList.Items.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show(strings.SETTINGS_RESTORED_MSG);
        }

        private void ImportSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(strings.NOT_IMPLEMENTED_MSG);
        }

        private void ExportSettingsBTN_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(strings.NOT_IMPLEMENTED_MSG);
        }

        private void ChangeLanguage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(strings.NOT_IMPLEMENTED_MSG);
        }
    }
}
