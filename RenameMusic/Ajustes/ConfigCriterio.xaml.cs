using RenameMusic.N39;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Configuración.xaml
    /// </summary>
    public partial class ConfigCriterio : Window
    {
        public ConfigCriterio()
        {
            try
            {
                InitializeComponent();
                criterioElegido.Text = Properties.Settings.Default.criterioCfg;
                simbolos.Text += "\n" + Properties.Resources.Simbols;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Aplicar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Chequear si el criterio es valido
                
                string[] tags = { "<tn>", "<t>", "<a>", "<aAt>", "<At>", "<yr>" };
                string criterioParaChequear = criterioElegido.Text;
                bool contieneUnTag = false;

                foreach (var tag in tags)
                {
                    if (criterioParaChequear.Contains(tag))
                    {
                        contieneUnTag = true;
                        string newtag = tag.Replace("<", "_").Replace(">", "_");
                        criterioParaChequear = criterioParaChequear.Replace(tag, newtag);
                    }
                }

                // Si no hay simbolos extraños y existe al menos 1 tag
                if (FunctionsN39.IsValidFileName(criterioParaChequear) && contieneUnTag)
                {
                    // Guarda cambios en la configuración de la app
                    Properties.Settings.Default["criterioCfg"] = criterioElegido.Text;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Ajuste guardado", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
                else if (!contieneUnTag)
                {
                    MessageBox.Show("Debe contener al menos 1 tag el criterio", "No permitido", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    string mensaje = "El criterio contiene caracteres invalidos y no pueden ser usados como nombre de archivo";
                    MessageBox.Show(mensaje, "No permitido", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // Descarta los cambios
            Close();
        }
    }
}
