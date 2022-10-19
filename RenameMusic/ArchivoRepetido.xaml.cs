using RenameMusic.N39;
using System;
using System.IO;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for ArchivoRepetido.xaml
    /// </summary>
    public partial class ArchivoRepetido : Window
    {
        private MusicFile musicFile = new MusicFile();
        private TagLib.File music;
        private string nuevoNombreConRuta = "";

        public ArchivoRepetido(MusicFile pMusicFile, TagLib.File pMusic, string pNuevoNombreConRuta)
        {
            try
            {
                InitializeComponent();
                nombreArchivo.Content = pMusic.Name;
                nuevoNombreArchivo.Content = pNuevoNombreConRuta + "." + pMusicFile.Formato;
                musicFile = pMusicFile;
                music = pMusic;
                nuevoNombreConRuta = pNuevoNombreConRuta;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reemplazar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Antes hay que verificar si el archivo todavia existe por las dudas
                // TODO: revisar si es necesario
                if (File.Exists(nuevoNombreConRuta + "." + musicFile.Formato))
                {
                    File.Delete(nuevoNombreConRuta + "." + musicFile.Formato);
                    File.Move(music.Name, nuevoNombreConRuta + "." + musicFile.Formato);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Close();
            }
        }

        private void Omitir_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Renombrar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int num = 2;
                while (File.Exists(nuevoNombreConRuta + " " + "(" + num + ")" + "." + musicFile.Formato))
                {
                    num += 1; // se incrementa en 1 en cada ciclo
                }

                File.Move(music.Name, nuevoNombreConRuta + " " + "(" + num + ")" + "." + musicFile.Formato);

                string mensaje = "El nombre del archivo\n" + nuevoNombreConRuta + "." + musicFile.Formato +
                    "\n" + "Fue renombrado como \n" + nuevoNombreConRuta + " " + "(" + num + ")" + "." + musicFile.Formato + "\n";

                MessageBox.Show(mensaje, "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Close();
            }
        }

        private void RepetirEleccion_Check(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.noAbrirVentanaDeAR = (bool)repetirEleccion.IsChecked;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
