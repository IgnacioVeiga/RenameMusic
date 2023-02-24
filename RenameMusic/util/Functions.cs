using Microsoft.WindowsAPICodePack.Dialogs;
using RenameMusic.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace RenameMusic.Util
{
    public static class MyFunctions
    {
        public static List<string> GetFilePaths(string path, bool includeSubFolders)
        {
            List<string> list = new();
            try
            {
                SearchOption searchOption = includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                string[] type = new string[] { ".mp3", ".m4a", ".ogg", ".flac" };
                list = Directory.GetFiles(path, "*.*", searchOption)
                    .Where(file => type.Any(t => file.EndsWith(t, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                list = new();
            }
            return list;
        }

        /// <summary>
        /// Common dialog for selecting files or folders
        /// </summary>
        /// <param name="isFolderPicker">"True" to select folders or "false" to select files.</param>
        /// <returns></returns>
        public static string[] ShowFPickerDialog(bool isFolderPicker)
        {
            CommonOpenFileDialog dialog = new()
            {
                AllowNonFileSystemItems = true,
                Multiselect = true,
                EnsurePathExists = true,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };

            if (isFolderPicker)
            {
                dialog.IsFolderPicker = true;
                dialog.Title = Strings.ADD_FOLDER;
            }
            else
            {
                dialog.IsFolderPicker = false;
                dialog.Title = Strings.ADD_FILE;
            }

            return dialog.ShowDialog() == CommonFileDialogResult.Ok
                ? dialog.FileNames.Where(fn => !string.IsNullOrEmpty(fn)).ToArray()
                : null;
        }
    }
}
