using RenameMusic.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /* El objetivo de la app es poder renombrar archivos mp3 por sus "tags" según
         * un criterio definido por el usuario o no (uno predeterminado).
         * Por ejemplo: Si tengo una canción con el nombre "AUD-01230101-WA0123.mp3" pero
         * esta misma tiene "tags", entonces puedo decidir que con estos se llame según
         * el siguiente orden: Pista-Nombre-Artista.mp3, pero tengo que hacerlo con muchas
         * canciones (ya es engorroso con una sola). Esa es la utilidad de esta app, quizas
         * en un futuro se pueda modificar los "tags" desde esta misma app en grandes cantidades.
         * 
         * Se debe poder:
         * Mostar una lista con todos los archivos compatibles
         * Ordenar por diversas formas (nombre, tamaño, carpeta, duración, etc)
         * Agregar más de una carpeta y eliminar tambien
         * Aquellos archivos que no contengan "tags" discriminarlos en otra pestaña
         * Enseñar los nombres de los archivos y en un lado sus futuros nombres
         * Definir la posición de los "tags" como nombre
         * Tener un criterio predeterminado para las posiciones
         * Reproducir el archivo desde esta app o una externa
         */

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string generarId()
        {
            // lo uso para generar un id en formato de string, unico y no nulo
            // este solo debe coincidir entre una carpeta y los archivos contenidos de la misma
            return Guid.NewGuid().ToString("N");
        }

        public static string NormalizeFileName(string fileName)
        {
            // Esta funcion se usa para cuando tengamos que renombrar a un archivo
            // Nos aseguramos que tenga solamente los caracteres permitidos por Windows
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(
                 new string(Path.GetInvalidFileNameChars())
            );
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(fileName, invalidRegStr, "_");
        }

        private void Ruta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // muesta dialogo para seleccion de carpetas
                CommonOpenFileDialog dialog = new CommonOpenFileDialog
                {
                    AllowNonFileSystemItems = true,
                    IsFolderPicker = true,
                    Multiselect = true,
                    Title = "Agregar carpeta/s",
                    EnsurePathExists = true,
                    DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) // carpeta de musica por defecto
                };

                // si da click en "Ok" y no hay rutas nulas, continua
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok && dialog.FileNames.All(fn => !string.IsNullOrWhiteSpace(fn)))
                {
                    // hace lista de carpetas seleccionadas
                    List<string> carpetasSeleccionadas = dialog.FileNames.ToList();
                    for (int i = 0; i < carpetasSeleccionadas.Count; i++)
                    {
                        //genero un Id unico y no nulo
                        string id = generarId();

                        // agregar carpeta
                        listaCarpetas.Items.Add(new CarpetaDTO
                        {
                            IdCanciones = id,
                            Ruta = carpetasSeleccionadas[i]
                        });

                        // Toma lista de archivos mp3, m4a, flac y wav en cada carpeta (pero sin subcarpetas)
                        string[] arrayMP3 = Directory.GetFiles(carpetasSeleccionadas[i], "*.mp3");
                        string[] arrayM4A = Directory.GetFiles(carpetasSeleccionadas[i], "*.m4a");
                        string[] arrayFLAC = Directory.GetFiles(carpetasSeleccionadas[i], "*.flac");
                        string[] arrayWAV = Directory.GetFiles(carpetasSeleccionadas[i], "*.wav");
                        string[] arrayOGG = Directory.GetFiles(carpetasSeleccionadas[i], "*.ogg");
                        string[] arrayDeCanciones = arrayMP3.Union(arrayM4A).Union(arrayFLAC).Union(arrayWAV).Union(arrayOGG).ToArray();

                        // Verifica si hay canciones en ese array
                        if (arrayDeCanciones.Length > 0)
                        {
                            for (int x = 0; x < arrayDeCanciones.Length; x++) // siendo "x" la posicion del vector a trabajar
                            {
                                // tomamos datos del mp3
                                TagLib.File cancion = TagLib.File.Create(arrayDeCanciones[x]);

                                // obtengo el formato y nombre para usarlo más adelante
                                string archivo = arrayDeCanciones[x].Substring(arrayDeCanciones[x].LastIndexOf(@"\") + 1);
                                string nombre = archivo.Remove(archivo.LastIndexOf("."));
                                string formato = archivo.Substring(archivo.LastIndexOf(".") + 1);

                                // si existe título en sus tags
                                if (!string.IsNullOrWhiteSpace(cancion.Tag.Title))
                                {
                                    // agregamos la cancion a la lista CON tags
                                    listaCancionesCT.Items.Add(new CancionDTO
                                    {
                                        Activo = true,
                                        IdCarpeta = id, // TODO: generar de forma unica y no nula ni cero
                                        NombreActual = nombre,
                                        //NuevoNombre = NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedArtists), // TODO: preparar segun el criterio
                                        Formato = formato,
                                        Titulo = cancion.Tag.Title,
                                        Album = cancion.Tag.Album,
                                        Artista = cancion.Tag.JoinedPerformers,
                                        Duracion = cancion.Properties.Duration,
                                        AlbumArtista = cancion.Tag.JoinedAlbumArtists
                                    });
                                }
                                else // si no tiene título en sus tags
                                {
                                    // agregamos la cancion a la lista SIN tags
                                    listaCancionesST.Items.Add(new CancionDTO
                                    {
                                        Activo = true,
                                        IdCarpeta = id,
                                        NombreActual = nombre,
                                        Formato = formato,
                                        Duracion = cancion.Properties.Duration
                                    });
                                }

                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Renombrar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // si existen carpetas
                if (listaCarpetas.Items.Count > 0)
                {
                    // revisar carpetas
                    for (int pos = 0; pos < listaCarpetas.Items.Count; pos++)
                    {
                        // definimos la carpeta a trabajar
                        CarpetaDTO carpeta = listaCarpetas.Items[pos] as CarpetaDTO;

                        // por seguridad verifico esto
                        if (carpeta != null)
                        {
                            // Verificar cantidad de canciones
                            if (listaCancionesCT.Items.Count > 0)
                            {
                                for (int x = 0; x < listaCancionesCT.Items.Count; x++)
                                {
                                    CancionDTO archivo = listaCancionesCT.Items[x] as CancionDTO;
                                    // tambien, por seguridad verifico esto
                                    if (archivo != null)
                                    {
                                        if (archivo.Activo && archivo.IdCarpeta == carpeta.IdCanciones) // solo trabaja con las canciones que dejamos los "checkboxes" marcados
                                        {
                                            string antiguoNombreConRuta = carpeta.Ruta + @"\" + archivo.NombreActual;
                                            TagLib.File cancion = TagLib.File.Create(antiguoNombreConRuta + "." + archivo.Formato);

                                            // TODO: esto hay que prepararlo segun el criterio seleccionado
                                            string nuevoNombreConRuta = carpeta.Ruta + @"\";
                                            if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedPerformers))
                                            {
                                                nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedPerformers);
                                            }
                                            else if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedAlbumArtists))
                                            {
                                                nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedAlbumArtists);
                                            }
                                            else
                                            {
                                                nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title);
                                            }

                                            // antes hay que verificar si el nuevo nombre no coincide con el anterior para evitar errores
                                            if ((nuevoNombreConRuta + "." + archivo.Formato).ToLower() != (antiguoNombreConRuta + "." + archivo.Formato).ToLower())
                                            {
                                                // verifico que no exista archivo con el nuevo nombre
                                                if (!File.Exists(nuevoNombreConRuta + "." + archivo.Formato))
                                                {
                                                    // cambiar el nombre del archivo
                                                    File.Move(antiguoNombreConRuta + "." + archivo.Formato, nuevoNombreConRuta + "." + archivo.Formato);
                                                }
                                                /*
                                                 * Si existe un archivo con el mismo nombre le doy a elegir al usuario:
                                                 * Reemplazar, Omitir o Renombrar
                                                */
                                                else
                                                {
                                                    ArchivoRepetido VentanaArchivoRepetido = new ArchivoRepetido(archivo, cancion, nuevoNombreConRuta);
                                                    VentanaArchivoRepetido.ShowDialog();

                                                    // TODO: agregar botón para repetir el paso elegido en los proximos casos
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("No se pudo encontrar una canción", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("Sin archivos compatibles en la ruta \n" + ruta, "Atención", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                        else
                        {
                            MessageBox.Show("No se encontró una carpeta", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                        }
                    }
                }
                else
                {
                    MessageBox.Show("No hay carpetas para trabajar. \n Utilice la función \"Agregar carpeta\" para continuar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                listaCancionesCT.Items.Clear();
                listaCancionesST.Items.Clear();
                listaCarpetas.Items.Clear();
                MessageBox.Show("Tarea finalizada", "Listo!", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                listaCancionesCT.Items.Clear();
                listaCancionesST.Items.Clear();
                listaCarpetas.Items.Clear();
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void activo_Click(object sender, RoutedEventArgs e)
        {
            // marcar si queremos renombar X cancion
        }

        private void quitar_Click(object sender, RoutedEventArgs e)
        {
            // quita la carpeta y sus canciones de sus respectivas listas
        }
    }
}
