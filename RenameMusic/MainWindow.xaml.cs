using RenameMusic.N39;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using TagLib;
using System.IO;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string ExceptionMsg = "Error, por favor notificar al desarrollador";

        /*
        El objetivo de la app es poder renombrar archivos mp3 por sus "tags" según
        un criterio definido por el usuario o no (uno predeterminado).
        Por ejemplo: Si tengo una canción con el nombre "AUD-01230101-WA0123.mp3" pero
        esta misma tiene "tags", entonces puedo decidir que con estos se llame según
        el siguiente orden: NroDePista-Titulo-Artista.mp3, pero tengo que hacerlo con muchas
        canciones (ya es engorroso con una sola). Esa es la utilidad de esta app, quizas
        en un futuro se pueda modificar los "tags" desde esta misma app en grandes cantidades.
        
        Se debe poder:
        Mostar una lista con todos los archivos compatibles.                            [Listo]
        Aquellos archivos que no contengan "tags" discriminarlos en otra pestaña.       [Listo]
        Agregar más de una carpeta y eliminar tambien.                                  [Listo]
        Enseñar los nombres de los archivos y en un lado sus futuros nombres.           [Listo]
        Definir la posición de los "tags" como nombre.                                  [Listo]
        Tener un criterio predeterminado para las posiciones.                           [Listo]
        Mostrar caratulas de los archivos.                                              [Listo]
        Ordenar la lista por diversas formas (nombre, carpeta, duración, etc).
        Reproducir el archivo desde esta app o una externa.
        Impedir que se repitan archivos y/o directorios.
        Arreglar los tamaños de los elementos.
        Agregar temas personalizados.
        */

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.criterioCfg))
                {
                    Properties.Settings.Default["criterioCfg"] = "<tn>. <t> - <a>";
                    Properties.Settings.Default.Save();
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
                MessageBox.Show(ex.ToString(), ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
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
                foreach (string folderPath in selectedFoldersList.FindAll(f => MyFunctions.GetFilePaths(f).Count > 1))
                {
                    // Toma lista de archivos con formato mp3, m4a, flac y ogg en cada carpeta
                    var musicFileList = MyFunctions.GetFilePaths(folderPath);

                    // Si tengo archivos de música, debo agregar la carpeta a su correspondiente tabla
                    // Por ahora se genera un id en formato de string, unico y no nulo
                    // El ID solo debe coincidir entre una carpeta y sus archivos contenidos en la misma
                    // TODO: borrar la linea de a continuacion una vez implementada la base de datos, si fuese necesario
                    MusicFolder carpetaItem = new MusicFolder
                    {
                        CancionesId = Guid.NewGuid().ToString("N"),
                        Ruta = folderPath
                    };
                    listaCarpetas.Items.Add(carpetaItem);

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
                MessageBox.Show(ex.ToString(), ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenombrarArchivos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listaCarpetas.Items.IsEmpty)
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
                foreach (MusicFolder carpetaItem in listaCarpetas.Items)
                {
                    foreach (MusicFile archivo in listaCancionesCT.Items)
                    {
                        // Solo se trabaja con los items con los "checkboxes" marcados, tambíen hay que asegurarse que todavia existe el archivo a trabajar
                        if (archivo.Activo && archivo.CarpetaId == carpetaItem.CancionesId && System.IO.File.Exists(carpetaItem.Ruta + @"\" + archivo.NombreActual + "." + archivo.Formato))
                        {
                            string antiguoNombreConRuta = carpetaItem.Ruta + @"\" + archivo.NombreActual;
                            // TODO: REVISAR QUE LA VARIABLE "archivo.NuevoNombre" TENGA TEXTO
                            string nuevoNombreConRuta = carpetaItem.Ruta + @"\" + archivo.NuevoNombre;

                            // Antes hay que verificar si el nuevo nombre no coincide con el anterior para evitar errores
                            if ((nuevoNombreConRuta).ToLower() != (antiguoNombreConRuta).ToLower())
                            {
                                // Verifico si ya existe un archivo con el nuevo nombre
                                if (System.IO.File.Exists(nuevoNombreConRuta + "." + archivo.Formato))
                                {
                                    // Si existe un archivo con el mismo nombre le doy a elegir al usuario: Reemplazar, Omitir o Renombrar
                                    ArchivoRepetido VentanaArchivoRepetido = new ArchivoRepetido(
                                        archivo,
                                        TagLib.File.Create(antiguoNombreConRuta + "." + archivo.Formato),
                                        nuevoNombreConRuta
                                    );
                                    VentanaArchivoRepetido.ShowDialog();
                                }
                                else
                                {
                                    // Cambiar el nombre del archivo
                                    System.IO.File.Move(antiguoNombreConRuta + "." + archivo.Formato, nuevoNombreConRuta + "." + archivo.Formato);
                                }
                            }
                        }
                    }
                }
            }

            // TODO: generar un archivo de log con todos los errores y encriptarlo
            catch (IOException)
            {
                string msg = "No puedo encontrar al menos una de las canciones en la lista";
                MessageBox.Show(msg, "Problema detectado :(", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                listaCancionesCT.Items.Clear();
                listaCancionesST.Items.Clear();
                listaCarpetas.Items.Clear();

                MessageBox.Show("Tarea finalizada", "Listo!", MessageBoxButton.OK);
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

                for (int i = 0; i < listaCarpetas.Items.Count;)
                {
                    if (((MusicFolder)listaCarpetas.Items[i]).CancionesId == id)
                    {
                        listaCarpetas.Items.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigCriterio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigTemplate config = new ConfigTemplate();
                config.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportarAjustes_Click(object sender, RoutedEventArgs e)
        {
            // TODO: para los proximos ajustes a añadir
            MessageBox.Show("Aún no implementado");
        }

        private void ExportarAjustes_Click(object sender, RoutedEventArgs e)
        {
            // TODO: para los proximos ajustes a añadir
            MessageBox.Show("Aún no implementado");
        }

        private void ReestablecerAjustes_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["criterioCfg"] = "<tn>. <t> - <a>";
            Properties.Settings.Default.Save();
            MessageBox.Show("Ajustes borrados. Usando ajustes predeterminados", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
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
                IPicture pic = ((MusicFile)((ListView)sender).SelectedItem).Pictures[0];

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
