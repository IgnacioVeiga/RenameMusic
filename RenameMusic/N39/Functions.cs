using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace RenameMusic.N39
{
    public static class FunctionsN39
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

        public static string GetNewName(TagLib.File song, string ruta)
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

            foreach (var tag in tags)
            {
                if (fileName.Contains(tag))
                {

                    switch (tag)
                    {
                        case "<tn>":
                            fileName = fileName.Replace(tag, song.Tag.Track.ToString());
                            break;
                        case "<t>":
                            fileName = fileName.Replace(tag, song.Tag.Title);
                            break;
                        case "<a>":
                            fileName = fileName.Replace(tag, song.Tag.Album);
                            break;
                        case "<aAt>":
                            fileName = fileName.Replace(tag, song.Tag.JoinedAlbumArtists);
                            break;
                        case "<At>":
                            fileName = fileName.Replace(tag, song.Tag.JoinedPerformers);
                            break;
                        case "<yr>":
                            fileName = fileName.Replace(tag, song.Tag.Year.ToString());
                            break;
                        default:
                            break;
                    }
                }
            }

            return ruta + NormalizeFileName(fileName);
        }

        public static string CheckCriterio()
        {
            string crtcfg = Properties.Settings.Default.criterioCfg;
            return crtcfg;
        }

        public static void ShowProblemsList(List<string> problems)
        {
            string msg = string.Join("\n", problems);
            MessageBox.Show(msg, "Problemas detectados :(", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public static TagLibResultN39 CreateMusicObj(string rutaArchivo, List<string> problems)
        {
            TagLibResultN39 result = new TagLibResultN39();
            try
            {
                // Creo un objeto archivo/cancion y tomo los datos
                result.File = TagLib.File.Create(rutaArchivo);
            }
            catch (TagLib.CorruptFileException)
            {
                result.File = null;
                problems.Add("No se añadirá a la lista el siguiente archivo corrupto o incompatible: " + rutaArchivo);
            }
            return result;
        }
    }
}
