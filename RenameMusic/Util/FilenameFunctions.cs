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

            if (string.IsNullOrWhiteSpace(audioTags.Title))
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
                            fileName = fileName.Replace(tag, audioTags.Track.ToString());
                            break;
                        case "<t>":
                            fileName = fileName.Replace(tag, audioTags.Title);
                            break;
                        case "<a>":
                            fileName = fileName.Replace(tag, audioTags.Album);
                            break;
                        case "<aAt>":
                            fileName = fileName.Replace(tag, audioTags.JoinedAlbumArtists);
                            break;
                        case "<At>":
                            fileName = fileName.Replace(tag, audioTags.JoinedPerformers);
                            break;
                        case "<yr>":
                            fileName = fileName.Replace(tag, audioTags.Year.ToString());
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
