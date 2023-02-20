using RenameMusic.Lang;
using RenameMusic.Properties;
using System;
using System.Windows;

namespace RenameMusic
{
    /// <summary>
    /// Interaction logic for ConfigTemplate.xaml
    /// </summary>
    public partial class ConfigTemplate : Window
    {
        public ConfigTemplate()
        {
            try
            {
                InitializeComponent();
                template.Text = Settings.Default.DefaultTemplate;
                simbols.Text = "<tn> = Track Number\r\n<t> = Title song\r\n<a> = Album\r\n<aAt> = Album Artist\r\n<At> = Artist\r\n<yr> = Year";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Chequear si el criterio es valido

                string[] tags = { "<tn>", "<t>", "<a>", "<aAt>", "<At>", "<yr>" };
                string criterioParaChequear = template.Text;
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
                if (MyFunctions.IsValidFileName(criterioParaChequear) && contieneUnTag)
                {
                    // Guarda cambios en la configuración de la app
                    Settings.Default.DefaultTemplate = template.Text;
                    Settings.Default.Save();
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
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            // Descarta los cambios
            Close();
        }
    }
}
