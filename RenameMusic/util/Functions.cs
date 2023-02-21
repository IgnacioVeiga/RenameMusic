using Microsoft.WindowsAPICodePack.Dialogs;
using RenameMusic.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using TagLib;

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
                return new List<string>();
            }
            return list;
        }

        public static string[] ShowFolderPickerDialog()
        {
            CommonOpenFileDialog folderDialog = new()
            {
                AllowNonFileSystemItems = true,
                IsFolderPicker = true,
                Multiselect = true,
                Title = Strings.ADD_FOLDER,
                EnsurePathExists = true,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                return folderDialog.FileNames.Where(fn => !string.IsNullOrEmpty(fn)).ToArray();
            else return null;
        }
    }
}
