using Microsoft.WindowsAPICodePack.Dialogs;
using RenameMusic.Lang;
using RenameMusic.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using TagLib;

namespace RenameMusic
{
    public static class MyFunctions
    {
        public static MusicFile GetMusicFile(string path)
        {
            try
            {
                // Creo un objeto archivo/cancion y tomo los datos
                TagLib.File mFile = TagLib.File.Create(path);
                if (mFile is null) return null;

                //AudioFile audioFile = new()
                //{
                //    Active = true,
                //    NewName = FilenameFunctions.GetNewName(mFile),
                //    Duration = mFile.Properties.Duration,

                //    // GetDirectoryName('C:\MyDir\MySubDir\myfile.ext') returns 'C:\MyDir\MySubDir'
                //    Folder = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar,
                //    FilePath = path
                //};

                return new MusicFile()
                {
                    Activo = true,                                          // Por defecto su "checkbox" en la lista está marcado
                    Id = Guid.NewGuid().ToString("N"),                      // Un ID generado automaticamente, TODO: cambiar esto ya mencionado arriba
                    NombreActual = Path.GetFileNameWithoutExtension(path),  // Nombre del archivo, sin formato ni ruta de archivo
                    Formato = Path.GetExtension(path),                      // El formato pero sin el "." del principio
                    Duracion = mFile.Properties.Duration,                   // Se visualiza en formato hh:mm:ss
                    NuevoNombre = FilenameFunctions.GetNewName(mFile),      // Nuevo nombre del archivo
                    Titulo = mFile.Tag.Title,
                    Album = mFile.Tag.Album,
                    Artista = mFile.Tag.JoinedPerformers,
                    AlbumArtista = mFile.Tag.JoinedAlbumArtists,
                    Pictures = mFile.Tag.Pictures
                };
            }
            catch (CorruptFileException)
            {
                return null;
            }
        }

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

        public static List<string> SelectAndListFolders()
        {
            CommonOpenFileDialog folderDialog = new()
            {
                // TODO: reemplazar este dialogo por el propio en creación
                AllowNonFileSystemItems = true,
                IsFolderPicker = true,
                Multiselect = true,
                Title = Strings.ADD_FOLDER_TITLE,
                EnsurePathExists = true,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                return folderDialog.FileNames.Where(fn => !string.IsNullOrWhiteSpace(fn)).ToList();
            else return null;
        }
    }
}
