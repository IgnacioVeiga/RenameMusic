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
        private SongN39 archivo = new SongN39();
        private TagLib.File cancion = null;
        private string nuevoNombreConRuta = "";

        public ArchivoRepetido(SongN39 pArchivo, TagLib.File pCancion, string pNuevoNombreConRuta)
        {
            try
            {
                InitializeComponent();
                nombreArchivo.Content = pCancion.Name;
                nuevoNombreArchivo.Content = pNuevoNombreConRuta + "." + pArchivo.Formato;
                archivo = pArchivo;
                cancion = pCancion;
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
                if (File.Exists(nuevoNombreConRuta + "." + archivo.Formato))
                {
                    File.Delete(nuevoNombreConRuta + "." + archivo.Formato);
                    File.Move(cancion.Name, nuevoNombreConRuta + "." + archivo.Formato);
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
                while (File.Exists(nuevoNombreConRuta + " " + "(" + num + ")" + "." + archivo.Formato))
                {
                    num += 1; // se incrementa en 1 en cada ciclo
                }

                File.Move(cancion.Name, nuevoNombreConRuta + " " + "(" + num + ")" + "." + archivo.Formato);

                string mensaje = "El nombre del archivo\n" + nuevoNombreConRuta + "." + archivo.Formato +
                    "\n" + "Fue renombrado como \n" + nuevoNombreConRuta + " " + "(" + num + ")" + "." + archivo.Formato + "\n";

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
