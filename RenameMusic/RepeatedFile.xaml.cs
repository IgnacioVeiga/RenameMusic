using System;
using System.IO;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for RepeatedFile.xaml
    /// </summary>
    public partial class RepeatedFile : Window
    {

        public RepeatedFile(string pOldFileName, string pNewFileName)
        {
            try
            {
                InitializeComponent();
                currentFileName.Content = pOldFileName;
                newFileName.Content = pNewFileName;
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
                // Verificar si el archivo sigue existiendo luego de esta pausa.
                if (File.Exists((string)newFileName.Content))
                {
                    File.Delete((string)newFileName.Content);
                    File.Move((string)currentFileName.Content, (string)newFileName.Content);
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

        //private void Renombrar_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        int num = 2;
        //        while (File.Exists(newFileNameAndPath + " " + "(" + num + ")" + "." + musicFile.Formato))
        //        {
        //            num += 1; // se incrementa en 1 en cada ciclo
        //        }

        //        File.Move(music.Name, newFileNameAndPath + " " + "(" + num + ")" + "." + musicFile.Formato);

        //        string mensaje = "El nombre del archivo\n" + newFileNameAndPath + "." + musicFile.Formato +
        //            "\n" + "Fue renombrado como \n" + newFileNameAndPath + " (" + num + ")." + musicFile.Formato + "\n";

        //        MessageBox.Show(mensaje, "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //    finally
        //    {
        //        Close();
        //    }
        //}

        private void RepetirEleccion_Check(object sender, RoutedEventArgs e)
        {
            //repetirEleccion.IsChecked;
        }
    }
}
