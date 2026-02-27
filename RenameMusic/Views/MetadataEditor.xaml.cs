using Microsoft.Win32;
using RenameMusic.Resources.Languages;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TagLib;

namespace RenameMusic.Views
{
    /// <summary>
    /// Interaction logic for MetadataEditor.xaml
    /// </summary>
    public partial class MetadataEditor : Window
    {
        private readonly string _filePath;
        private string? _newImageFilePath;
        private bool _imageChanged;

        public MetadataEditor(string path)
        {
            InitializeComponent();
            _filePath = path;
            try
            {
                using TagLib.File file = TagLib.File.Create(_filePath);

                AudioTitle.Text = file.Tag.Title ?? string.Empty;
                Artist.Text = file.Tag.JoinedPerformers ?? string.Empty;
                Album.Text = file.Tag.Album ?? string.Empty;
                AlbumArtist.Text = file.Tag.JoinedAlbumArtists ?? string.Empty;
                Year.Text = file.Tag.Year > 0 ? file.Tag.Year.ToString() : string.Empty;
                Genres.Text = file.Tag.JoinedGenres ?? string.Empty;
                Comment.Text = file.Tag.Comment ?? string.Empty;

                if (file.Tag.Pictures.Length > 0)
                {
                    Pictures.Source = Multimedia.GetBitmapImage(file.Tag.Pictures[0].Data.Data);
                    PicturesInfo.Text = file.Tag.Pictures[0].MimeType;
                }
                else
                {
                    PicturesInfo.Text = string.Empty;
                }

                FileInfo.Text = $"{file.Properties.AudioBitrate}kbps {file.Properties.AudioSampleRate}hz";
            }
            catch (Exception)
            {
                MessageBox.Show(Strings.FILE_NOT_FOUND_MSG, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    InitialDirectory = Path.GetDirectoryName(_filePath) ?? Environment.CurrentDirectory,
                    CheckFileExists = true,
                    Filter = $"{Strings.SUPPORTED_FILES}*.jpg;*.jpeg;*.png;*.gif;*.webp|JPEG|*.jpg;*.jpeg|PNG|*.png|GIF|*.gif|WEBP|*.webp"
                };

                // In .mp3 files, does the cover art have to be a 64kb .png format?

                if (imagePicker.ShowDialog() == true)
                {
                    _newImageFilePath = imagePicker.FileName;
                    _imageChanged = true;
                    try
                    {
                        Pictures.Source = Multimedia.GetBitmapImage(imagePicker.FileName);
                    }
                    catch (Exception ex)
                    {
                        _imageChanged = false;
                        _newImageFilePath = null;
                        MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using TagLib.File file = TagLib.File.Create(_filePath);
                file.Tag.Title = string.IsNullOrWhiteSpace(AudioTitle.Text) ? null : AudioTitle.Text.Trim();
                file.Tag.Performers = SplitValues(Artist.Text);
                file.Tag.Album = string.IsNullOrWhiteSpace(Album.Text) ? null : Album.Text.Trim();
                file.Tag.AlbumArtists = SplitValues(AlbumArtist.Text);
                file.Tag.Year = uint.TryParse(Year.Text, out uint year) ? year : 0;
                file.Tag.Genres = SplitValues(Genres.Text);
                file.Tag.Comment = string.IsNullOrWhiteSpace(Comment.Text) ? null : Comment.Text.Trim();

                if (_imageChanged && !string.IsNullOrWhiteSpace(_newImageFilePath) && File.Exists(_newImageFilePath))
                {
                    // TODO: Preserve existing additional pictures and support multi-image editing.
                    file.Tag.Pictures = new IPicture[] { new Picture(_newImageFilePath) };
                }

                file.Save();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string[] SplitValues(string rawValues)
        {
            return rawValues
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
