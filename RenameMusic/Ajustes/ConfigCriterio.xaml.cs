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
        private string backupCriterioCfg = Properties.Settings.Default.criterioCfg;

        public ConfigCriterio()
        {
            try
            {
                InitializeComponent();
                criterioElegido.Text = backupCriterioCfg;
                simbolos.Text += "\n" + Properties.Resources.Simbols;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Aplicar_Click(object sender, RoutedEventArgs e)
        {
            // Guarda cambios en la configuración de la app
            Properties.Settings.Default["criterioCfg"] = criterioElegido.Text;
            MessageBox.Show("Ajuste guardado", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // Descarta los cambios y restaura los ajustes anteriores
            Properties.Settings.Default["criterioCfg"] = backupCriterioCfg;
            Close();
        }
    }
}
