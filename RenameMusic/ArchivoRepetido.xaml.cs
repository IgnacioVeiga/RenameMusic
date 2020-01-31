using RenameMusic.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for ArchivoRepetido.xaml
    /// </summary>
    public partial class ArchivoRepetido : Window
    {
        private CancionDTO archivo = new CancionDTO();
        private TagLib.File cancion = null;
        private string nuevoNombreConRuta = "";

        public ArchivoRepetido(CancionDTO pArchivo, TagLib.File pCancion, string pNuevoNombreConRuta)
        {
            try
            {
                InitializeComponent();
                nombreArchivo.Content = pCancion.Name;
                nuevoNombreArchivo.Content = pNuevoNombreConRuta + "." + pArchivo.Formato;
                archivo = pArchivo;
                cancion = pCancion;
                nuevoNombreConRuta = pNuevoNombreConRuta;
                Topmost = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reemplazar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // antes hay que verificar si el archivo todavia existe por las dudas
                // TODO: revisar si es necesario
                if (File.Exists(nuevoNombreConRuta + "." + archivo.Formato))
                {
                    File.Delete(nuevoNombreConRuta + "." + archivo.Formato);
                    File.Move(cancion.Name, nuevoNombreConRuta + "." + archivo.Formato);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
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
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }
    }
}
