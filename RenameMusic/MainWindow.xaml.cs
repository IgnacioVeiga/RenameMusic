using RenameMusic.N39;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Input;
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
        public static bool noAbrirVentanaDeAR;

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
            // Para mostrar al final del proceso los errores ocurridos
            List<string> problems = new List<string>();
            try
            {
                // Seleccionamos carpetas
                List<string> selectedFoldersList = FunctionsN39.SelectAndListFolders();
                if (selectedFoldersList == null) return;

                // Ahora hay que tomar de los directorios el contenido que buscamos
                // Esto puede tardar mucho, hay que hacerlo en segundo plano
                //LoadingN39 loadingWindow = new LoadingN39(0, selectedFoldersList.Count);

                foreach (string aFolderPath in selectedFoldersList)
                {
                    // Toma lista de archivos con formato mp3, m4a, flac y ogg en cada carpeta
                    string[] arrayDeCanciones = FunctionsN39.GetAnArrayOfFilePaths(aFolderPath);

                    // Verifico si existe al menos un item
                    if (!arrayDeCanciones.Any())
                    {
                        MessageBox.Show("No se encontró ningún archivo de música", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Si tengo archivos de música, debo agregar la carpeta a su correspondiente tabla
                    // Por ahora se genera un id en formato de string, unico y no nulo
                    // El ID solo debe coincidir entre una carpeta y sus archivos contenidos en la misma
                    // TODO: borrar la linea de a continuacion una vez implementada la base de datos, si fuese necesario
                    string id = Guid.NewGuid().ToString("N");
                    FolderN39 carpetaItem = new FolderN39
                    {
                        CancionesId = id,
                        Ruta = aFolderPath
                    };
                    listaCarpetas.Items.Add(carpetaItem);

                    // Ahora debo agregar cada archivo de música a su correspondiente lista
                    foreach (string rutaArchivo in arrayDeCanciones)
                    {
                        // Creo el objeto con la libreria TagLib en un metodo aparte para capturar errores
                        TagLibResultN39 result = FunctionsN39.CreateMusicObj(rutaArchivo, problems);

                        // No continuar con el mismo archivo si hay errores con la creación del objeto TagLib
                        if (result.Problems == null && result.File != null)
                        {
                            // Obtengo el nombre y formato para usarlo más adelante
                            string archivo = rutaArchivo.Substring(rutaArchivo.LastIndexOf(@"\") + 1);
                            string nombre = archivo.Remove(archivo.LastIndexOf("."));
                            string formato = archivo.Substring(archivo.LastIndexOf(".") + 1);

                            SongN39 song = new SongN39
                            {
                                Activo = true,                                  // Por defecto su "checkbox" en la lista está marcado
                                Id = Guid.NewGuid().ToString("N"),              // Un ID generado automaticamente, TODO: cambiar esto ya mencionado arriba
                                CarpetaId = id,                                 // Se le asocia el ID de su carpeta
                                NombreActual = nombre,                          // Nombre del archivo, sin formato ni ruta de archivo
                                Formato = formato,                              // El formato pero sin el "." del principio
                                Duracion = result.File.Properties.Duration      // TODO: buscar como darle forma con algún pipe
                            };

                            // Según si el tag "Title" no es nulo o un espacio vacio, el item va en una tabla u otra
                            if (FunctionsN39.HasTitleTag(result.File))
                            {
                                // Agrego el item a la tabla "SIN tags"
                                listaCancionesST.Items.Add(song);
                            }
                            else
                            {
                                song.NuevoNombre = FunctionsN39.GetNewName(result.File);     // Nuevo nombre del archivo

                                #region Tags
                                song.Titulo = result.File.Tag.Title;
                                song.Album = result.File.Tag.Album;
                                song.Artista = result.File.Tag.JoinedPerformers;
                                song.AlbumArtista = result.File.Tag.JoinedAlbumArtists;
                                song.Pictures = result.File.Tag.Pictures;
                                #endregion Tags

                                // Agrego el item a la tabla "CON tags"
                                listaCancionesCT.Items.Add(song);
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
            finally
            {
                // Si existe por lo menos un problema en la lista, enseñamos un mensaje con todos los problemas detectados
                if (problems.Any())
                {
                    FunctionsN39.ShowProblemsList(problems);
                }
            }
        }

        private void RenombrarArchivos_Click(object sender, RoutedEventArgs e)
        {
            // Para mostrar al final del proceso los errores ocurridos
            List<string> problems = new List<string>();
            try
            {
                if (listaCarpetas.Items.IsEmpty)
                {
                    string msg1 = "No hay carpetas para trabajar.";
                    string msg2 = "\nUtilice la función \"Agregar carpeta\" para continuar.";
                    MessageBox.Show(msg1 + msg2, "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                // Reviso la tabla de carpetas
                foreach (var carpetaItem in listaCarpetas.Items)
                {
                    // Creo un objeto la carpeta para trabajar
                    FolderN39 carpeta = carpetaItem as FolderN39;

                    // Esto se verifica por seguridad, que el objeto no sea nulo
                    if (carpeta == null)
                    {
                        problems.Add("No se encontró una carpeta: " + carpetaItem);
                    }
                    else
                    {
                        // Verifico que existan items en la tabla de canciones con tags
                        if (listaCancionesCT.Items.IsEmpty)
                        {
                            problems.Add("Sin archivos compatibles en la ruta \n" + carpeta.Ruta);
                        }
                        else
                        {
                            for (int pos = 0; pos < listaCancionesCT.Items.Count; pos++)
                            {
                                SongN39 archivo = listaCancionesCT.Items[pos] as SongN39;

                                if (archivo == null)
                                {
                                    problems.Add("No se pudo encontrar una canción");
                                }
                                else
                                {
                                    // Solo se trabaja con los items con los "checkboxes" marcados
                                    if (archivo.Activo && archivo.CarpetaId == carpeta.CancionesId)
                                    {
                                        // Antes de continuar, hay que asegurarse que todavia existe el archivo a trabajar
                                        if (System.IO.File.Exists(carpeta.Ruta + @"\" + archivo.NombreActual + "." + archivo.Formato))
                                        {
                                            string antiguoNombreConRuta = carpeta.Ruta + @"\" + archivo.NombreActual;
                                            TagLib.File cancion = TagLib.File.Create(antiguoNombreConRuta + "." + archivo.Formato);

                                            string nuevoNombreConRuta = carpeta.Ruta + @"\" + FunctionsN39.GetNewName(cancion);

                                            // Antes hay que verificar si el nuevo nombre no coincide con el anterior para evitar errores
                                            if ((nuevoNombreConRuta + "." + archivo.Formato).ToLower() != (antiguoNombreConRuta + "." + archivo.Formato).ToLower())
                                            {
                                                // Verifico si ya existe un archivo con el nuevo nombre
                                                if (System.IO.File.Exists(nuevoNombreConRuta + "." + archivo.Formato))
                                                {
                                                    // Si existe un archivo con el mismo nombre le doy a elegir al usuario: Reemplazar, Omitir o Renombrar
                                                    ArchivoRepetido VentanaArchivoRepetido = new ArchivoRepetido(archivo, cancion, nuevoNombreConRuta);

                                                    if (noAbrirVentanaDeAR)
                                                        VentanaArchivoRepetido.ShowDialog();
                                                }
                                                else
                                                {
                                                    // Cambiar el nombre del archivo
                                                    System.IO.File.Move(antiguoNombreConRuta + "." + archivo.Formato, nuevoNombreConRuta + "." + archivo.Formato);
                                                }
                                            }
                                        }
                                        // Si el archivo no existe, guardo el mensaje de error para más adelante
                                        else
                                        {
                                            string msg = "No existe el archivo: " + carpeta.Ruta + @"\" + archivo.NombreActual + "." + archivo.Formato;
                                            problems.Add(msg);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // TODO: generar un archivo de log con todos los errores y encriptarlo
            catch (System.IO.IOException)
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

                // Si existe por lo menos un problema en la lista, enseñamos un mensaje con todos los problemas detectados
                if (problems.Any())
                {
                    FunctionsN39.ShowProblemsList(problems);
                }

                MessageBox.Show("Tarea finalizada", "Listo!", MessageBoxButton.OK);
            }
        }

        private void quitar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtengo el id de los archivos asociados a la carpeta seleccionada
                string id = ((FolderN39)((Button)sender).DataContext).CancionesId;

                for (int i = 0; i < listaCancionesCT.Items.Count;)
                {
                    if (((SongN39)listaCancionesCT.Items[i]).CarpetaId.Equals(id))
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
                    if (((SongN39)listaCancionesST.Items[i]).CarpetaId == id)
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
                    if (((FolderN39)listaCarpetas.Items[i]).CancionesId == id)
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
                ConfigCriterio config = new ConfigCriterio();
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
            if ((SongN39)((ListView)sender).SelectedItem == null)
            {
                pictures.Source = new BitmapImage(new Uri("./icon.ico", UriKind.Relative));
                return;
            }

            if (((SongN39)((ListView)sender).SelectedItem).Pictures.Length >= 1)
            {
                IPicture pic = ((SongN39)((ListView)sender).SelectedItem).Pictures[0];

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
