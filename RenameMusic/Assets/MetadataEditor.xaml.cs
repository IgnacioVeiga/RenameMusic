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
        private readonly TagLib.Tag OriginalTags;
        private readonly string filepath;

        public MetadataEditor(string path)
        {
            InitializeComponent();
            filepath = path;
            try
            {
                TagLib.File file = TagLib.File.Create(filepath);
                OriginalTags = file.Tag;
                AudioTitle.Text = OriginalTags.Title;
                Artist.Text = OriginalTags.JoinedPerformers;
                Album.Text = OriginalTags.Album;
                AlbumArtist.Text = OriginalTags.JoinedAlbumArtists;
                Year.Text = OriginalTags.Year.ToString();
                Genres.Text = OriginalTags.JoinedGenres;
                Comment.Text = OriginalTags.Comment;

                Pictures.Source = Multimedia.GetBitmapImage(OriginalTags.Pictures[0].Data.Data);
                PicturesInfo.Text = OriginalTags.Pictures[0].MimeType;

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
                    // Change image
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
