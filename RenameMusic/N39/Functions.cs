using System.IO;

namespace RenameMusic.N39
{
    public class FunctionsN39
    {

        /*
        * Esta funcion se usa para cuando tengamos que renombrar a un archivo
        * Nos aseguramos que tenga solamente los caracteres permitidos por Windows
        */
        public string NormalizeFileName(string fileName)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(
                 new string(Path.GetInvalidFileNameChars())
            );
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(fileName, invalidRegStr, "_");
        }
    }
}
