using System.IO;
using System.Text.RegularExpressions;

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

        public static string RenombrarArchivoCriterioDefault(TagLib.File cancion, string nuevoNombreConRuta)
        {
            if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedPerformers))
            {
                return nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedPerformers);
            }
            else if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedAlbumArtists))
            {
                return nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedAlbumArtists);
            }
            else
            {
                return nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title);
            }
        }
    }
}
