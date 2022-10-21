using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

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


        /// <summary>
        /// Sirve para generar un nuevo nombre a un archivo según el criterio definido
        /// </summary>
        /// <param name="pMusic">Objeto con tags del archivo</param>
        /// <returns>Retorna el nuevo nombre del archivo sin la ruta</returns>
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
            string fileName = Properties.Settings.Default.criterioCfg;

            if (string.IsNullOrWhiteSpace(pMusic.Tag.Title))
            {
                return null;
            }

            foreach (var tag in tags)
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

        public static void ShowProblemsList(List<string> problems)
        {
            string msg = string.Join("\n", problems);
            MessageBox.Show(msg, "Problemas detectados :(", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

                string[] exts = new string[] { ".mp3", ".m4a", ".flac", ".ogg", };
                string[] array = Directory.GetFiles(path, "*.*", searchOption)
                    .Where(file => exts.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                // Por seguridad filtro aquellos items que son nulos
                return array.Where(fn => fn != null).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MainWindow.ExceptionMsg, MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<string>();
            }
        }

        public static List<string> SelectAndListFolders()
        {
            // Sirve para mostrar el dialogo selector de carpetas
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog
            {
                // TODO: reemplazar este dialogo por el propio en creación
                AllowNonFileSystemItems = true,
                IsFolderPicker = true,
                Multiselect = true,
                Title = "Agregar carpeta/s",
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
                if ((newFileName).ToLower() != (oldFileName).ToLower())
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
            catch (IOException)
            {
                MessageBox.Show("No puedo encontrar al menos uno de los archivos de la lista");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
