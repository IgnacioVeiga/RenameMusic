using Microsoft.Win32;
using RenameMusic.Resources.Languages;
using System.IO;
using System.Windows;

namespace RenameMusic.Services
{
    public interface IFilePickerService
    {
        IReadOnlyList<string> PickFiles();
        IReadOnlyList<string> PickFolders();
    }

    public sealed class FilePickerService : IFilePickerService
    {
        /// <summary>
        /// Opens the file picker limited to supported audio formats.
        /// </summary>
        public IReadOnlyList<string> PickFiles()
        {
            OpenFileDialog fileDialog = new()
            {
                ValidateNames = true,
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                CheckFileExists = true,
                Title = Strings.ADD_FILE,
                Filter = $"{Strings.SUPPORTED_FILES}|*.mp3;*.m4a;*.ogg;*.flac|" +
                         $"{L("FILE_FILTER_MP3_LABEL", "MPEG Audio Layer III (MP3)")}|*.mp3|" +
                         $"{L("FILE_FILTER_M4A_LABEL", "MPEG-4 Audio (M4A)")}|*.m4a|" +
                         $"{L("FILE_FILTER_OGG_LABEL", "Vorbis (OGG)")}|*.ogg|" +
                         $"{L("FILE_FILTER_FLAC_LABEL", "Free Lossless Audio Codec (FLAC)")}|*.flac"
            };

            if (fileDialog.ShowDialog() is false)
            {
                return [];
            }

            return fileDialog.FileNames
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public IReadOnlyList<string> PickFolders()
        {
            OpenFolderDialog folderDialog = new()
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
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string L(string key, string fallback)
        {
            return Strings.ResourceManager.GetString(key, Strings.Culture) ?? fallback;
        }
    }
}
