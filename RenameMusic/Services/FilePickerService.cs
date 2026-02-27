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
        public IReadOnlyList<string> PickFiles()
        {
            OpenFileDialog fileDialog = new()
            {
                ValidateNames = true,
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                CheckFileExists = true,
                Title = Strings.ADD_FILE,
                Filter = $"{Strings.SUPPORTED_FILES}|*.mp3;*.m4a;*.ogg;*.flac|MPEG Audio Layer III (MP3)|*.mp3|MPEG-4 Audio (M4A)|*.m4a|Vorbis (OGG)|*.ogg|Free Lossless Audio Codec (FLAC)|*.flac"
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
    }
}
