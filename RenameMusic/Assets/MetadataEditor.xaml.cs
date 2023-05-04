using Microsoft.Win32;
using RenameMusic.Util;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace RenameMusic.Assets
{
    /// <summary>
    /// Interaction logic for MetadataEditor.xaml
    /// </summary>
    public partial class MetadataEditor : Window
    {
        private readonly string filepath;

        public MetadataEditor(string path)
        {
            InitializeComponent();
            filepath = path;
            try
            {
                TagLib.File file = TagLib.File.Create(filepath);

                AudioTitle.Text = file.Tag.Title;
                Artist.Text = file.Tag.JoinedPerformers;
                Album.Text = file.Tag.Album;
                AlbumArtist.Text = file.Tag.JoinedAlbumArtists;
                Year.Text = file.Tag.Year.ToString();
                Genres.Text = file.Tag.JoinedGenres;
                Comment.Text = file.Tag.Comment;

                Pictures.Source = Multimedia.GetBitmapImage(file.Tag.Pictures[0].Data.Data);
                PicturesInfo.Text = file.Tag.Pictures[0].MimeType;

                FileInfo.Text = $"{file.Properties.AudioBitrate}kbps {file.Properties.AudioSampleRate}hz";
            }
            catch (Exception)
            {
                Close();
            }
        }

        private void Pictures_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                OpenFileDialog imagePicker = new()
                {
                    ValidateNames = true,
                    Multiselect = false,
                    InitialDirectory = Path.GetDirectoryName(filepath) + Path.DirectorySeparatorChar,
                    CheckFileExists = true,
                    Filter = "JPG|*.jpg|JPEG|*jpeg|PNG|*.png|GIF|*.gif|WEBP|*.webp"
                };

                if (imagePicker.ShowDialog() == true)
                {
                    // ToDo: Change image
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            TagLib.File file = TagLib.File.Create(filepath);
            file.Tag.Title = AudioTitle.Text;
            file.Tag.Performers = Artist.Text.Split(";");
            file.Tag.Album = Album.Text;
            file.Tag.AlbumArtists = AlbumArtist.Text.Split(";");
            file.Tag.Year = uint.Parse(Year.Text);
            file.Tag.Genres = Genres.Text.Split(";");
            file.Tag.Comment = Comment.Text;

            // ToDo: Set pictures too

            try
            {
                file.Save();
                DialogResult = true;
            }
            catch (Exception)
            {
                DialogResult = false;
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
