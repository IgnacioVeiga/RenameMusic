using Microsoft.WindowsAPICodePack.Dialogs;
using RenameMusic.N39;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /*
              El objetivo de la app es poder renombrar archivos mp3 por sus "tags" según
              un criterio definido por el usuario o no (uno predeterminado).
              Por ejemplo: Si tengo una canción con el nombre "AUD-01230101-WA0123.mp3" pero
              esta misma tiene "tags", entonces puedo decidir que con estos se llame según
              el siguiente orden: NroDePista-Titulo-Artista.mp3, pero tengo que hacerlo con muchas
              canciones (ya es engorroso con una sola). Esa es la utilidad de esta app, quizas
              en un futuro se pueda modificar los "tags" desde esta misma app en grandes cantidades.

              Se debe poder:
              Mostar una lista con todos los archivos compatibles. [Listo]
              Aquellos archivos que no contengan "tags" discriminarlos en otra pestaña. [Listo]
              Agregar más de una carpeta y eliminar tambien. [Parcial]
              Enseñar los nombres de los archivos y en un lado sus futuros nombres. [Parcial]
              Ordenar la lista por diversas formas (nombre, tamaño, carpeta, duración, etc).
              Definir la posición de los "tags" como nombre.
              Tener un criterio predeterminado para las posiciones.
              Reproducir el archivo desde esta app o una externa.
        */

        public static bool noAbrirVentanaDeAR = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                /*
                 * TODO: leer base de datos y completar las tablas,
                 * validar todos los items de cada pestaña que aún existen
                 * (En caso de que no existan se deben resaltar en color rojo)
                 */
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // muesta dialogo para seleccion de carpetas
                CommonOpenFileDialog dialog = new CommonOpenFileDialog
                {
                    // TODO: reemplazar el dialogo de abajo por un buscador propio de carpetas
                    AllowNonFileSystemItems = true,
                    IsFolderPicker = true,
                    Multiselect = true,
                    Title = "Agregar carpeta/s",
                    EnsurePathExists = true,
                    DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) // carpeta de musica por defecto
                };

                // Condicion: debo heber hecho clic en "Ok" y no tener rutas nulas
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok && dialog.FileNames.All(fn => !string.IsNullOrWhiteSpace(fn)))
                {
                    // Ahora se hace la lista de carpetas seleccionadas
                    List<string> carpetasSeleccionadas = dialog.FileNames.ToList();
                    for (int i = 0; i < carpetasSeleccionadas.Count; i++)
                    {
                        /* 
                         * Esto ahora genera un id en formato de string, unico y no nulo
                         * El ID solo debe coincidir entre una carpeta y los archivos contenidos de la misma
                         */
                        // TODO: borrar la linea de a continuacion una vez implementada la base de datos
                        string id = Guid.NewGuid().ToString("N");

                        // Creo el objeto carpeta
                        FolderN39 carpetaItem = new FolderN39
                        {
                            CancionesId = id,
                            Ruta = carpetasSeleccionadas[i]
                        };
                        // El objeto carpeta es añadido a la lista
                        listaCarpetas.Items.Add(carpetaItem);

                        // Con esto defino si quiero incluir subdirectotios en la busqueda
                        bool incluirSubdirectorios = false;
                        SearchOption searchOption = incluirSubdirectorios ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                        // Toma lista de archivos con formato mp3, m4a, flac, wav y ogg en cada carpeta
                        string[] arrayMP3 = Directory.GetFiles(carpetasSeleccionadas[i], "*.mp3", searchOption);
                        string[] arrayM4A = Directory.GetFiles(carpetasSeleccionadas[i], "*.m4a", searchOption);
                        string[] arrayFLAC = Directory.GetFiles(carpetasSeleccionadas[i], "*.flac", searchOption);
                        string[] arrayWAV = Directory.GetFiles(carpetasSeleccionadas[i], "*.wav", searchOption);
                        string[] arrayOGG = Directory.GetFiles(carpetasSeleccionadas[i], "*.ogg", searchOption);

                        // Ahora todos los array de arriba se unen en uno solo
                        string[] arrayDeCanciones = arrayMP3.Union(arrayM4A).Union(arrayFLAC).Union(arrayWAV).Union(arrayOGG).ToArray();

                        // Verifico si hay items en ese array de canciones
                        if (arrayDeCanciones.Length > 0)
                        {
                            for (int x = 0; x < arrayDeCanciones.Length; x++) // siendo "x" la posicion del vector a trabajar
                            {
                                //TODO: el archivos debe ser de audio 100% valido y no dañado, nada de pdf renombrado a mp3
                                // Creo un objeto archivo/cancion y tomo los datos del que estoy analizando en el vector
                                TagLib.File cancion = TagLib.File.Create(arrayDeCanciones[x]);

                                // Obtengo el nombre y formato para usarlo más adelante
                                string archivo = arrayDeCanciones[x].Substring(arrayDeCanciones[x].LastIndexOf(@"\") + 1);
                                string nombre = archivo.Remove(archivo.LastIndexOf("."));
                                string formato = archivo.Substring(archivo.LastIndexOf(".") + 1);

                                // Condicion: debe existir un título en sus tags
                                if (!string.IsNullOrWhiteSpace(cancion.Tag.Title))
                                {
                                    // Creo un objeto cancion para que la tabla lo pueda usar
                                    SongN39 cancionItem = new SongN39
                                    {
                                        Activo = true,
                                        Id = Guid.NewGuid().ToString("N"),
                                        CarpetaId = id,
                                        NombreActual = nombre,
                                        //NuevoNombre = NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedArtists), // TODO: preparar segun el criterio
                                        Formato = formato,
                                        Titulo = cancion.Tag.Title,
                                        Album = cancion.Tag.Album,
                                        Artista = cancion.Tag.JoinedPerformers,
                                        Duracion = cancion.Properties.Duration,
                                        AlbumArtista = cancion.Tag.JoinedAlbumArtists
                                    };

                                    // Agrego la cancion a la lista CON tags
                                    listaCancionesCT.Items.Add(cancionItem);

                                    // Creo la tabla si no existe aún
                                    //DbNasho database = new DbNasho();
                                    //var connection = database.CreateConnection();
                                    //database.CreateTable(connection);
                                    //database.InsertData(connection, "Canciones", cancionItem);
                                    //database.InsertData(connection, "Carpetas", carpetaItem);
                                }
                                else // Si no cumple la condicion (Existencia de titulo en sus tags), hay que agregarlo en la otra tabla
                                {
                                    // Creo un objeto cancion para que la tabla lo pueda usar, este usa el mismo DTO pero con menos atributos
                                    SongN39 cancionItem = new SongN39
                                    {
                                        Activo = true,
                                        Id = Guid.NewGuid().ToString("N"),
                                        CarpetaId = id,
                                        NombreActual = nombre,
                                        Formato = formato,
                                        Duracion = cancion.Properties.Duration
                                    };

                                    // Agrego la cancion a la lista SIN tags
                                    listaCancionesST.Items.Add(cancionItem);
                                }
                            }
                        }
                        else
                        {
                            listaCarpetas.Items.Remove(carpetaItem);
                        }
                    }

                }
            }

            // TODO: generar un archivo de log con todos los errores y encriptarlo
            catch (TagLib.CorruptFileException)
            {
                string msg = "Al menos uno de tus archivos está dañado, por lo que no puedo manejar esta carpeta";
                MessageBox.Show(msg, "Problema detectado", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                listaCancionesCT.Items.Clear();
                listaCancionesST.Items.Clear();
                listaCarpetas.Items.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenombrarArchivos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listaCarpetas.Items.Count <= 0)
                {
                    MessageBox.Show("No hay carpetas para trabajar.\nUtilice la función \"Agregar carpeta\" para continuar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                // Reviso la tabla de carpetas
                for (int pos = 0; pos < listaCarpetas.Items.Count; pos++)
                {
                    // Creo un objeto la carpeta para trabajar
                    FolderN39 carpeta = listaCarpetas.Items[pos] as FolderN39;

                    // Esto se verifica por seguridad, que el objeto no sea nulo
                    if (carpeta == null)
                    {
                        // TODO: mandar este mensaje una sola vez y con la lista de carpetas
                        MessageBox.Show("No se encontró una carpeta", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        // TODO: VOLVER A VERIFICAR SI LOS ARCHIVOS EXISTEN
                        // Verifico que existan items en la tabla de canciones con tags
                        if (listaCancionesCT.Items.Count > 0)
                        {
                            for (int x = 0; x < listaCancionesCT.Items.Count; x++)
                            {
                                SongN39 archivo = listaCancionesCT.Items[x] as SongN39;

                                if (archivo == null)
                                {
                                    // TODO: mandar este mensaje una sola vez y con la lista de canciones
                                    MessageBox.Show("No se pudo encontrar una canción", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    if (archivo.Activo && archivo.CarpetaId == carpeta.CancionesId) // solo trabaja con las canciones que dejamos los "checkboxes" marcados
                                    {
                                        string antiguoNombreConRuta = carpeta.Ruta + @"\" + archivo.NombreActual;
                                        TagLib.File cancion = TagLib.File.Create(antiguoNombreConRuta + "." + archivo.Formato);

                                        // TODO: esto hay que prepararlo segun el criterio seleccionado
                                        string nuevoNombreConRuta = carpeta.Ruta + @"\";
                                        if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedPerformers))
                                        {
                                            nuevoNombreConRuta += FunctionsN39.NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedPerformers);
                                        }
                                        else if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedAlbumArtists))
                                        {
                                            nuevoNombreConRuta += FunctionsN39.NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedAlbumArtists);
                                        }
                                        else
                                        {
                                            nuevoNombreConRuta += FunctionsN39.NormalizeFileName(cancion.Tag.Title);
                                        }

                                        // Antes hay que verificar si el nuevo nombre no coincide con el anterior para evitar errores
                                        if ((nuevoNombreConRuta + "." + archivo.Formato).ToLower() != (antiguoNombreConRuta + "." + archivo.Formato).ToLower())
                                        {
                                            // Verifico si ya existe un archivo con el nuevo nombre
                                            if (File.Exists(nuevoNombreConRuta + "." + archivo.Formato))
                                            {
                                                // Si existe un archivo con el mismo nombre le doy a elegir al usuario:
                                                // Reemplazar, Omitir o Renombrar

                                                ArchivoRepetido VentanaArchivoRepetido = new ArchivoRepetido(archivo, cancion, nuevoNombreConRuta);

                                                if (noAbrirVentanaDeAR)
                                                    VentanaArchivoRepetido.ShowDialog();
                                            }
                                            else
                                            {
                                                // Cambiar el nombre del archivo
                                                File.Move(antiguoNombreConRuta + "." + archivo.Formato, nuevoNombreConRuta + "." + archivo.Formato);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Sin archivos compatibles en la ruta \n" + ruta, "Atención", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }

                MessageBox.Show("Tarea finalizada", "Listo!", MessageBoxButton.OK);
            }

            // TODO: generar un archivo de log con todos los errores y encriptarlo
            catch (IOException)
            {
                string msg = "No puedo encontrar al menos una de las canciones en la lista";
                MessageBox.Show(msg, "Problema detectado", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                listaCancionesCT.Items.Clear();
                listaCancionesST.Items.Clear();
                listaCarpetas.Items.Clear();
            }
        }

        private void activo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: marcar si queremos renombar X cancion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void quitar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: quitar la carpeta y sus canciones de sus respectivas listas
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
