using Microsoft.WindowsAPICodePack.Dialogs;
using RenameMusic.Lang;
using RenameMusic.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace RenameMusic
{
    public static class MyFunctions
    {

        /*
        * Esta funcion se usa para cuando tengamos que renombrar a un archivo
        * Nos aseguramos que tenga solamente los caracteres permitidos por Windows
        */
        public static string NormalizeFileName(string fileName)
        {
            string invalidChars = Regex.Escape(
                 new string(Path.GetInvalidFileNameChars())
            );
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(fileName, invalidRegStr, "_");
        }

        public static string GetNewName(TagLib.File pMusic)
        {
            /*
                <tn> = Track Number
                <t> = Title song
                <a> = Album
                <aAt> = Album Artist
                <At> = Artist
                <yr> = Year
             */
            string[] tags = { "<tn>", "<t>", "<a>", "<aAt>", "<At>", "<yr>" };
            string fileName = Settings.Default.DefaultTemplate;

            if (string.IsNullOrWhiteSpace(pMusic.Tag.Title))
            {
                return null;
            }

            foreach (string tag in tags)
            {
                if (fileName.Contains(tag))
                {
                    switch (tag)
                    {
                        case "<tn>":
                            fileName = fileName.Replace(tag, pMusic.Tag.Track.ToString());
                            break;
                        case "<t>":
                            fileName = fileName.Replace(tag, pMusic.Tag.Title);
                            break;
                        case "<a>":
                            fileName = fileName.Replace(tag, pMusic.Tag.Album);
                            break;
                        case "<aAt>":
                            fileName = fileName.Replace(tag, pMusic.Tag.JoinedAlbumArtists);
                            break;
                        case "<At>":
                            fileName = fileName.Replace(tag, pMusic.Tag.JoinedPerformers);
                            break;
                        case "<yr>":
                            fileName = fileName.Replace(tag, pMusic.Tag.Year.ToString());
                            break;
                        default:
                            break;
                    }
                }
            }

            if (!IsValidFileName(fileName))
            {
                fileName = NormalizeFileName(fileName);
            }

            return fileName;
        }

        public static bool IsValidFileName(string filename)
        {
            foreach (char item in Path.GetInvalidFileNameChars())
            {
                if (filename.Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        public static TagLib.File CreateMusicObj(string rutaArchivo)
        {
            try
            {
                // Creo un objeto archivo/cancion y tomo los datos
                return TagLib.File.Create(rutaArchivo);
            }
            catch (TagLib.CorruptFileException)
            {
                return null;
            }
        }

        public static List<string> GetFilePaths(string path, bool includeSubFolders)
        {
            try
            {
                SearchOption searchOption = includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                // TODO: añadir soporte a archivos ".flac" o con formato mayor a 3 caracteres
                string[] exts = new string[] { ".mp3", ".m4a", ".ogg", };
                string[] array = Directory.GetFiles(path, "*.*", searchOption)
                    .Where(file => exts.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                // Por seguridad filtro aquellos items que son nulos
                return array.Where(fn => fn != null).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<string>();
            }
        }

        public static List<string> SelectAndListFolders()
        {
            // Sirve para mostrar el dialogo selector de carpetas
            CommonOpenFileDialog folderDialog = new()
            {
                // TODO: reemplazar este dialogo por el propio en creación
                AllowNonFileSystemItems = true,
                IsFolderPicker = true,
                Multiselect = true,
                Title = Strings.ADD_FOLDER_TITLE,
                EnsurePathExists = true,

                // Carpeta de musica por defecto
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };

            // Muestro la ventana para seleccionar carpeta y cargamos datos si es ok
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return folderDialog.FileNames.Where(fn => !string.IsNullOrWhiteSpace(fn)).ToList();
            }
            return null;
        }

        public static void RenameFile(string oldFileName, string newFileName)
        {
            try
            {
                // Antes hay que verificar si el nuevo nombre no coincide con el anterior para evitar errores
                if (newFileName.ToLower() != oldFileName.ToLower())
                {
                    // Verifico si ya existe un archivo con el nuevo nombre
                    if (File.Exists(newFileName))
                    {
                        // Si existe un archivo con el mismo nombre le doy a elegir al usuario: Reemplazar, Omitir o Renombrar
                        RepeatedFile RepeatedFile = new(oldFileName, newFileName);
                        RepeatedFile.ShowDialog();
                    }
                    else
                    {
                        // Cambiar el nombre del archivo
                        File.Move(oldFileName, newFileName);
                    }
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
