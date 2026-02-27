using RenameMusic.Properties;
using RenameMusic.Resources.Languages;
using RenameMusic.Views;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RenameMusic
{
    internal static class FilenameFunctions
    {
        /// <summary>
        /// Use before renamed a file to make sure the new name contains characters allowed by Windows.
        /// </summary>
        /// <param name="fileName">Filename without extension.</param>
        public static string NormalizeFileName(string fileName)
        {
            string invalidChars = Regex.Escape(
                 new string(Path.GetInvalidFileNameChars())
            );
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(fileName, invalidRegStr, "_");
        }

        public static string GetNewName(TagLib.Tag audioTags)
        {
            string[] tags = ["<TrackNum>", "<Title>", "<Album>", "<AlbumArtist>", "<Artist>", "<Year>"];
            string fileName = Settings.Default.DefaultTemplate;

            foreach (string tag in tags)
            {
                if (!fileName.Contains(tag)) continue;
                switch (tag)
                {
                    case "<TrackNum>":
                        fileName = fileName.Replace(tag, audioTags.Track.ToString());
                        break;
                    case "<Title>":
                        fileName = fileName.Replace(tag, audioTags.Title);
                        break;
                    case "<Album>":
                        fileName = fileName.Replace(tag, audioTags.Album);
                        break;
                    case "<AlbumArtist>":
                        fileName = fileName.Replace(tag, audioTags.JoinedAlbumArtists);
                        break;
                    case "<Artist>":
                        fileName = fileName.Replace(tag, audioTags.JoinedPerformers);
                        break;
                    case "<Year>":
                        fileName = fileName.Replace(tag, audioTags.Year.ToString());
                        break;
                }
            }
            if (!IsValidFileName(fileName)) fileName = NormalizeFileName(fileName);

            return fileName;
        }

        public static bool IsValidFileName(string filename)
        {
            foreach (char item in Path.GetInvalidFileNameChars())
            {
                if (filename.Contains(item)) return false;
            }
            return true;
        }

        public static void RenameFile(string oldName, string newName)
        {
            if (!File.Exists(newName))
            {
                try
                {
                    // Rename it
                    File.Move(oldName, newName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                RepeatedFile dialog = new(oldName, newName);
                if (dialog.ShowDialog() != true || dialog.Result is null)
                {
                    return;
                }

                try
                {
                    switch (dialog.Result.Action)
                    {
                        case Models.ConflictResolutionAction.Replace:
                            File.Delete(newName);
                            File.Move(oldName, newName);
                            break;
                        case Models.ConflictResolutionAction.Skip:
                            break;
                        case Models.ConflictResolutionAction.RenameWithNumber:
                            int num = 2;
                            string dirAndFileName = Path.Combine(
                                Path.GetDirectoryName(newName) ?? string.Empty,
                                Path.GetFileNameWithoutExtension(newName));
                            string extension = Path.GetExtension(newName);
                            string candidate = $"{dirAndFileName} ({num}){extension}";
                            while (File.Exists(candidate))
                            {
                                num++;
                                candidate = $"{dirAndFileName} ({num}){extension}";
                            }

                            File.Move(oldName, candidate);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    internal static class Multimedia
    {
        internal static BitmapImage GetBitmapImage(byte[] buffer)
        {
            MemoryStream ms = new(buffer);
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            return bitmap;
        }

        internal static BitmapImage GetBitmapImage(string filepath)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filepath);
            bitmap.EndInit();

            return bitmap;
        }
    }

    // ToDo: Refactor this class
    public static class Picker
    {
        public static string[] ShowFolderPicker()
        {
            Microsoft.Win32.OpenFolderDialog folderDialog = new()
            {
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Title = Strings.ADD_FOLDER
            };

            if (folderDialog.ShowDialog() is false)
            {
                return [];
            }

            return folderDialog.FolderNames
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string[] ShowFilePicker()
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new()
            {
                ValidateNames = true,
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                CheckFileExists = true,
                Title = Strings.ADD_FILE,
                Filter = $"{Strings.SUPPORTED_FILES}|*.mp3;*.m4a;*.ogg;*.flac|MPEG Audio Layer III (MP3)|*.mp3|MPEG-4 Audio (M4A)|*.m4a|Vorbis (OGG)|*.ogg|Free Lossless Audio Codec (FLAC)|*.flac"
            };

            if (fileDialog.ShowDialog() is false) return [];

            return (string[])fileDialog.FileNames.Where(fn => !string.IsNullOrEmpty(fn)).ToArray();
        }

        public static string[] GetFilePaths(string path)
        {
            string[] filePaths = [];
            try
            {
                SearchOption searchOption = Settings.Default.IncludeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                string[] type = [".mp3", ".m4a", ".ogg", ".flac"];
                filePaths = Directory.GetFiles(path, "*.*", searchOption)
                    .Where(file => type.Any(t => file.EndsWith(t, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                filePaths = [];
            }
            return filePaths;
        }
    }
}
