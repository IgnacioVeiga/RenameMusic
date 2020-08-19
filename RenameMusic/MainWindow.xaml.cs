using Microsoft.WindowsAPICodePack.Dialogs;
using RenameMusic.N39;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string ExceptionMsg = "Error, por favor notificar al desarrollador";
        public static bool noAbrirVentanaDeAR = false;

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
                // Sirve para mostrar el dialogo selector de carpetas
                CommonOpenFileDialog dialog = new CommonOpenFileDialog
                {
                    // TODO: reemplazar este dialogo por el propio en creación
                    AllowNonFileSystemItems = true,
                    IsFolderPicker = true,
                    Multiselect = true,
                    Title = "Agregar carpeta/s",
                    EnsurePathExists = true,

                    // Carpeta de musica por defecto
                    DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
                };

                // Muestra la ventana para seleccionar carpeta y guarda el resultado.
                // A resultado me refiero a si la ventana fue cerrada, dieron clic en "cancelar",
                // una carpeta fue seleccionada correctamente, etcetera.
                CommonFileDialogResult resultDialog = dialog.ShowDialog();

                List<string> rutasDeCarpetasSeleccionadas = new List<string>();

                if (resultDialog != CommonFileDialogResult.Ok || dialog.FileNames.All(fn => string.IsNullOrWhiteSpace(fn)))
                {
                    MessageBox.Show("No hay carpeta/s cargada/s", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else if (dialog.FileNames.Any(fn => string.IsNullOrWhiteSpace(fn)))
                {
                    // Guardo en "carpetasSeleccionadas" todas aquellas carpetas que su "path" no sea nulo o un espacio vacio
                    rutasDeCarpetasSeleccionadas = dialog.FileNames.Where(fn => !string.IsNullOrWhiteSpace(fn)).ToList();
                }

                // Ahora se hace la lista de carpetas seleccionadas
                rutasDeCarpetasSeleccionadas = dialog.FileNames.ToList();

                foreach (string rutaDeCarpetaSeleccionada in rutasDeCarpetasSeleccionadas)
                {
                    // Con esto defino si quiero incluir subdirectotios en la busqueda de archivos
                    bool incluirSubdirectorios = false;
                    SearchOption searchOption = incluirSubdirectorios ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                    // Toma lista de archivos con formato mp3, m4a, flac, wav y ogg en cada carpeta
                    string[] arrayMP3 = Directory.GetFiles(rutaDeCarpetaSeleccionada, "*.mp3", searchOption);
                    string[] arrayM4A = Directory.GetFiles(rutaDeCarpetaSeleccionada, "*.m4a", searchOption);
                    string[] arrayFLAC = Directory.GetFiles(rutaDeCarpetaSeleccionada, "*.flac", searchOption);
                    string[] arrayOGG = Directory.GetFiles(rutaDeCarpetaSeleccionada, "*.ogg", searchOption);

                    // Ahora todos los array de arriba se unen en uno solo
                    string[] arrayDeCanciones = arrayMP3.Union(arrayM4A).Union(arrayFLAC).Union(arrayOGG).ToArray();

                    // Por seguridad filtro aquellos items que son nulos
                    arrayDeCanciones = arrayDeCanciones.Where(fn => fn != null).ToArray();

                    // Verifico si existe al menos un item
                    if (arrayDeCanciones.Any())
                    {
                        // Si tengo archivos de música, debo agregar la carpeta a su correspondiente tabla
                        // Por ahora se genera un id en formato de string, unico y no nulo
                        // El ID solo debe coincidir entre una carpeta y sus archivos contenidos en la misma
                        // TODO: borrar la linea de a continuacion una vez implementada la base de datos, si fuese necesario
                        string id = Guid.NewGuid().ToString("N");
                        FolderN39 carpetaItem = new FolderN39
                        {
                            CancionesId = id,
                            Ruta = rutaDeCarpetaSeleccionada
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

                                // Según si el tag "Title" no es nulo o un espacio vacio, el item va en una tabla u otra
                                if (string.IsNullOrWhiteSpace(result.File.Tag.Title))
                                {
                                    // Agrego el item a la tabla "SIN tags"
                                    listaCancionesST.Items.Add(new SongN39
                                    {
                                        Activo = true,                              // Por defecto su "checkbox" en la lista está marcado
                                        Id = Guid.NewGuid().ToString("N"),          // Un ID generado automaticamente, TODO: cambiar esto ya mencionado arriba
                                        CarpetaId = id,                             // Se le asocia el ID de su carpeta
                                        NombreActual = nombre,                      // Nombre del archivo, sin formato ni ruta de archivo
                                        Formato = formato,                          // El formato pero sin el "." del principio
                                        Duracion = result.File.Properties.Duration      // TODO: buscar como darle forma con algún pipe
                                    });
                                }
                                else
                                {
                                    // Agrego el item a la tabla "CON tags"
                                    listaCancionesCT.Items.Add(new SongN39
                                    {
                                        Activo = true,                              // Por defecto su "checkbox" en la lista está marcado
                                        Id = Guid.NewGuid().ToString("N"),          // Un ID generado automaticamente, TODO: cambiar esto ya mencionado arriba
                                        CarpetaId = id,                             // Se le asocia el ID de su carpeta
                                        NombreActual = nombre,                      // Nombre del archivo, sin formato ni ruta de archivo
                                                                                    // TODO: segun un criterio definido,
                                                                                    // el nombre futuro del archivo debe
                                                                                    // poder mostrarse en la tabla
                                        Formato = formato,                          // El formato pero sin el "." del principio
                                        Duracion = result.File.Properties.Duration,     // TODO: buscar como darle forma con algún pipe

                                        #region Tags
                                        Titulo = result.File.Tag.Title,
                                        Album = result.File.Tag.Album,
                                        Artista = result.File.Tag.JoinedPerformers,
                                        AlbumArtista = result.File.Tag.JoinedAlbumArtists
                                        #endregion Tags
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("No se encontró ningún archivo de música", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
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
                                        if (File.Exists(carpeta.Ruta + @"\" + archivo.NombreActual + "." + archivo.Formato))
                                        {
                                            string antiguoNombreConRuta = carpeta.Ruta + @"\" + archivo.NombreActual;
                                            TagLib.File cancion = TagLib.File.Create(antiguoNombreConRuta + "." + archivo.Formato);

                                            string nuevoNombreConRuta = FunctionsN39.GetNewName(cancion, carpeta.Ruta + @"\");

                                            // Antes hay que verificar si el nuevo nombre no coincide con el anterior para evitar errores
                                            if ((nuevoNombreConRuta + "." + archivo.Formato).ToLower() != (antiguoNombreConRuta + "." + archivo.Formato).ToLower())
                                            {
                                                // Verifico si ya existe un archivo con el nuevo nombre
                                                if (File.Exists(nuevoNombreConRuta + "." + archivo.Formato))
                                                {
                                                    // Si existe un archivo con el mismo nombre le doy a elegir al usuario: Reemplazar, Omitir o Renombrar
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

                // Si existe por lo menos un problema en la lista, enseñamos un mensaje con todos los problemas detectados
                if (problems.Any())
                {
                    FunctionsN39.ShowProblemsList(problems);
                }

                MessageBox.Show("Tarea finalizada", "Listo!", MessageBoxButton.OK);
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
                MessageBox.Show(ex.ToString(), ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void CargarAjustes_Click(object sender, RoutedEventArgs e)
        {
            // TODO: para los proximos ajustes a añadir
            MessageBox.Show("Ajustes cargados", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GuardarAjustes_Click(object sender, RoutedEventArgs e)
        {
            // TODO: para los proximos ajustes a añadir
            MessageBox.Show("Ajustes guardados", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BorrarAjustes_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["criterioCfg"] = "<tn>. <t> - <a>";
            Properties.Settings.Default.Save();
            MessageBox.Show("Ajustes borrados. Usando ajustes predeterminados", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
