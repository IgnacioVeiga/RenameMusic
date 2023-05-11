﻿using RenameMusic.Lang;
using RenameMusic.Properties;
using RenameMusic.Util;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RenameMusic.Assets
{
    /// <summary>
    /// Interaction logic for Template.xaml
    /// </summary>
    public partial class RenamingRule : Window
    {
        readonly string[] tags = { "<tn>", "<t>", "<a>", "<aAt>", "<At>", "<yr>" };

        public RenamingRule()
        {
            InitializeComponent();
            titleRequired.IsChecked = Settings.Default.TitleRequired;
            albumRequired.IsChecked = Settings.Default.AlbumRequired;
            albumArtistRequired.IsChecked = Settings.Default.AlbumArtistRequired;
            artistRequired.IsChecked = Settings.Default.ArtistRequired;
            renamingRule.Text = Settings.Default.DefaultTemplate;

            titleRequired.Content = $"<t> = {Strings.TITLE}";
            albumRequired.Content = $"<a> = {Strings.ALBUM}";
            albumArtistRequired.Content = $"<aAt> = {Strings.ALBUM_ARTIST}";
            artistRequired.Content = $"<At> = {Strings.ARTIST}";
            simbols.Text = $"<tn> = {Strings.TRACK_NUM}\r\n<yr> = {Strings.YEAR}";
        }

        private void ApplyBTN_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.TitleRequired = (bool)titleRequired.IsChecked;
            Settings.Default.AlbumRequired = (bool)albumRequired.IsChecked;
            Settings.Default.AlbumArtistRequired = (bool)albumArtistRequired.IsChecked;
            Settings.Default.ArtistRequired = (bool)artistRequired.IsChecked;
            Settings.Default.DefaultTemplate = renamingRule.Text;
            Settings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void CancelBTN_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ToDo: mostar el mensaje de "Warning" solo cuando sea necesario
        private void Rule_TextChanged(object sender, TextChangedEventArgs e)
        {
            // No debe ser un espacio vacio o similar
            if (string.IsNullOrWhiteSpace(renamingRule.Text))
            {
                ApplyBTN.IsEnabled = false;
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: {Strings.CANNOT_BE_EMPTY}";
                return;
            }

            // Debe contener por lo menos 1 tag
            // ToDo: también deben contener todos los marcados como obligatorios
            ApplyBTN.IsEnabled = tags.Any(tag => renamingRule.Text.Contains(tag));
            if (!ApplyBTN.IsEnabled)
            {
                // ToDo: cambiar el mensaje
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: Debe contener un tag minimo";
                return;
            }

            // Reemplazo solo los caracteres invalidos de los "tags", entonces cualquier otro caracter
            // invalido restante no forma parte de los "tags" y esto influye en el botón de aplicar
            string temp = renamingRule.Text;
            foreach (string tag in tags)
            {
                temp = temp.Replace(tag, tag.Replace("<", "_").Replace(">", "_"));
            }
            if (!FilenameFunctions.IsValidFileName(temp))
            {
                ApplyBTN.IsEnabled = false;
                // ToDo: revisar la traducciones
                WarningMSG.Text = $"{Strings.NOT_ALLOWED}: {Strings.INVALID_TEMPLATE_MSG}";
            }
            else
            {
                ApplyBTN.IsEnabled = true;
                WarningMSG.Text = "";
            }
        }
    }
}
