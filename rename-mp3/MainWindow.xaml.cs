using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace rename_mp3
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
            InitializeComponent();
        }

        private void Renombrar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Selección de carpeta
                // Verificar cantidad de archivos antes de agregar
                // Tomar lista de archivos mp3
                string[] filePaths;
                string path = @"D:/Music/";
                filePaths = Directory.GetFiles(path, "*.mp3");
                if (filePaths.Length > 0)
                {
                    for (int i = 0; i < filePaths.Length; i++)
                    {
                        // comprobar si existen "tags" y empezar a renombrar para probar
                        var tfile = TagLib.File.Create(filePaths[i]);
                        string title = tfile.Tag.Title;
                        string artist = tfile.Tag.JoinedAlbumArtists;
                        string newfilename = path + title + " - " + artist + ".mp3";

                        // verificar que el archivo a renombrar no exista
                        if (File.Exists(newfilename))
                        {
                            MessageBox.Show("Nombre repetido: \n" + newfilename, "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            // cambiar el nombre del archivo
                            File.Move(tfile.Name, newfilename);
                        }

                    }
                }
                else
                {
                    MessageBox.Show("Sin archivos compatibles en la ruta \n" + path, "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Ruta_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Todavia no hace nada ese botón", ":)", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
