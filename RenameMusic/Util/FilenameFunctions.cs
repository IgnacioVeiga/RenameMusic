using RenameMusic.Lang;
using RenameMusic.Properties;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace RenameMusic.Util
{
    internal static class FilenameFunctions
    {
        /// <summary>
        /// Esta funcion se usa para cuando tengamos que renombrar a un archivo
        /// Nos aseguramos que tenga solamente los caracteres permitidos por Windows
        /// </summary>
        /// <param name="fileName">Nombre del archivo sin extensión</param>
        /// <returns>El nombre del archivo pero normalizado</returns>
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
                    }
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

        public static void RenameFile(string oldFileName, string newFileName)
        {
            try
            {
                if (!File.Exists(oldFileName)) return;

                // Antes hay que verificar si el nuevo nombre no coincide con el anterior para evitar errores
                if (string.Equals(newFileName, oldFileName, StringComparison.OrdinalIgnoreCase))
                    return;

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
