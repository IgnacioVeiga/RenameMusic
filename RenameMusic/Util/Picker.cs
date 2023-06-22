using RenameMusic.Language;
using RenameMusic.Properties;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace RenameMusic.Util
{
    // ToDo: Refactor this class
    public static class Picker
    {
        public static string[] ShowFolderPicker()
        {
            Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog folderDialog = new()
            {
                IsFolderPicker = true,
                Multiselect = true,
                EnsurePathExists = true,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Title = Strings.ADD_FOLDER
            };

            return folderDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok
                ? folderDialog.FileNames.Where(fn => !string.IsNullOrEmpty(fn)).ToArray()
                : Array.Empty<string>();
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

            return (bool)fileDialog.ShowDialog()
                ? fileDialog.FileNames.Where(fn => !string.IsNullOrEmpty(fn)).ToArray()
                : Array.Empty<string>();
        }

        public static string[] GetFilePaths(string path)
        {
            string[] filePaths = Array.Empty<string>();
            try
            {
                SearchOption searchOption = Settings.Default.IncludeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                string[] type = new string[] { ".mp3", ".m4a", ".ogg", ".flac" };
                filePaths = Directory.GetFiles(path, "*.*", searchOption)
                    .Where(file => type.Any(t => file.EndsWith(t, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                filePaths = Array.Empty<string>();
            }
            return filePaths;
        }
    }
}
