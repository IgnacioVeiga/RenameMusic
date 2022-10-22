using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;
using RenameMusic.Lang;
using RenameMusic.Properties;

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
                if (string.IsNullOrWhiteSpace(Settings.Default.DefaultTemplate))
                {
                    Settings.Default.DefaultTemplate = "<tn>. <t> - <a>";
                    Settings.Default.Save();
                }

                infoTags.Text = "";
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

        /// <summary>
        /// Función que se ejecuta cuando se hace clic en el botón de "Añadir carpeta"
        /// </summary>
        private void AñadirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Seleccionamos carpetas
                var selectedFoldersList = MyFunctions.SelectAndListFolders();
                if (selectedFoldersList == null) return;

                // Ahora hay que tomar de los directorios el contenido que buscamos
                // Esto puede tardar mucho, hay que hacerlo en segundo plano
                foreach (string folderPath in selectedFoldersList.FindAll(f => MyFunctions.GetFilePaths(f, false).Count > 1))
                {
                    // Toma lista de archivos con formato mp3, m4a, flac y ogg en cada carpeta
                    var musicFileList = MyFunctions.GetFilePaths(folderPath, false);

                    // Si tengo archivos de música, debo agregar la carpeta a su correspondiente tabla
                    // Por ahora se genera un id en formato de string, unico y no nulo
                    // El ID solo debe coincidir entre una carpeta y sus archivos contenidos en la misma
                    // TODO: borrar la linea de a continuacion una vez implementada la base de datos, si fuese necesario
                    MusicFolder carpetaItem = new MusicFolder
                    {
                        CancionesId = Guid.NewGuid().ToString("N"),
                        Ruta = folderPath
                    };
                    folderList.Items.Add(carpetaItem);

                    // Ahora debo agregar cada archivo de música a su correspondiente lista
                    foreach (string rutaArchivo in musicFileList)
                    {
                        // Creo el objeto con la libreria TagLib en un metodo aparte para capturar errores
                        TagLib.File mFile = MyFunctions.CreateMusicObj(rutaArchivo);

                        if (mFile != null)
                        {

                            // Obtengo el nombre y formato para usarlo más adelante
                            string fileName = rutaArchivo.Substring(rutaArchivo.LastIndexOf(@"\") + 1);

                            MusicFile musicItem = new MusicFile
                            {
                                Activo = true,                                                  // Por defecto su "checkbox" en la lista está marcado
                                Id = Guid.NewGuid().ToString("N"),                              // Un ID generado automaticamente, TODO: cambiar esto ya mencionado arriba
                                CarpetaId = carpetaItem.CancionesId,                            // Se le asocia el ID de su carpeta
                                NombreActual = fileName.Remove(fileName.LastIndexOf(".")),      // Nombre del archivo, sin formato ni ruta de archivo
                                Formato = fileName.Substring(fileName.LastIndexOf(".") + 1),    // El formato pero sin el "." del principio
                                Duracion = mFile.Properties.Duration,                           // Se visualiza en formato hh:mm:ss
                                NuevoNombre = MyFunctions.GetNewName(mFile),                    // Nuevo nombre del archivo
                                Titulo = mFile.Tag.Title,
                                Album = mFile.Tag.Album,
                                Artista = mFile.Tag.JoinedPerformers,
                                AlbumArtista = mFile.Tag.JoinedAlbumArtists,
                                Pictures = mFile.Tag.Pictures
                            };

                            // Según si el nuevo nombre existe, entonces al menos tiene el tag de titulo
                            if (musicItem.NuevoNombre == null)
                            {
                                // Agrego el item a la tabla "SIN tags"
                                listaCancionesST.Items.Add(musicItem);
                            }
                            else
                            {
                                // Agrego el item a la tabla "CON tags"
                                listaCancionesCT.Items.Add(musicItem);
                            }
                        }
                    }
                }
            }

            // TODO: generar un archivo de log con todos los errores y encriptarlo
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenombrarArchivos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (folderList.Items.IsEmpty)
                {
                    string msg = "No hay carpetas para trabajar.\nUtilice la función \"Agregar carpeta\"para continuar.";
                    MessageBox.Show(msg, "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (listaCancionesCT.Items.IsEmpty)
                {
                    string msg = "No hay archivos CON Tags para trabajar.\nUtilice la función \"Agregar carpeta\"para continuar.";
                    MessageBox.Show(msg, "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                // Reviso la tabla de carpetas
                foreach (MusicFolder folderItem in folderList.Items)
                {
                    foreach (MusicFile mFileItem in listaCancionesCT.Items)
                    {
                        // Solo se trabaja con los items con los "checkboxes" marcados, tambíen hay que asegurarse que todavia existe el archivo a trabajar
                        if (mFileItem.Activo && mFileItem.CarpetaId == folderItem.CancionesId && File.Exists(folderItem.Ruta + @"\" + mFileItem.NombreActual + "." + mFileItem.Formato))
                        {
                            string oldFileName = folderItem.Ruta + @"\" + mFileItem.NombreActual + "." + mFileItem.Formato;
                            string newFileName = folderItem.Ruta + @"\" + mFileItem.NuevoNombre + "." + mFileItem.Formato;
                            MyFunctions.RenameFile(oldFileName, newFileName);
                        }
                    }
                }

                listaCancionesCT.Items.Clear();
                listaCancionesST.Items.Clear();
                folderList.Items.Clear();

                MessageBox.Show(strings.TASK_SUCCESFULL_MSG);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void quitar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtengo el id de los archivos asociados a la carpeta seleccionada
                string id = ((MusicFolder)((Button)sender).DataContext).CancionesId;

                for (int i = 0; i < listaCancionesCT.Items.Count;)
                {
                    if (((MusicFile)listaCancionesCT.Items[i]).CarpetaId.Equals(id))
                    {
                        listaCancionesCT.Items.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                for (int i = 0; i < listaCancionesST.Items.Count;)
                {
                    if (((MusicFile)listaCancionesST.Items[i]).CarpetaId == id)
                    {
                        listaCancionesST.Items.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                for (int i = 0; i < folderList.Items.Count;)
                {
                    if (((MusicFolder)folderList.Items[i]).CancionesId == id)
                    {
                        folderList.Items.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigTemplate config = new();
                config.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultTemplate = "<tn>. <t> - <a>";
            Settings.Default.Save();
            MessageBox.Show(strings.SETTINGS_RESTORED_MSG);
        }

        private void listaCancionesCT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pictures.Source = null;
            if ((MusicFile)((ListView)sender).SelectedItem == null)
            {
                pictures.Source = new BitmapImage(new Uri("./icon.ico", UriKind.Relative));
                return;
            }

            if (((MusicFile)((ListView)sender).SelectedItem).Pictures.Length >= 1)
            {
                TagLib.IPicture pic = ((MusicFile)((ListView)sender).SelectedItem).Pictures[0];

                // Load you image data in MemoryStream
                MemoryStream ms = new MemoryStream(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                // ImageSource for System.Windows.Controls.Image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                pictures.Source = bitmap;
            }
        }
    }
}
