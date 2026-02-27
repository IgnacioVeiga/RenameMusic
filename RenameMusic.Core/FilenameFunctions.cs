using System.IO;
using System.Text.RegularExpressions;

namespace RenameMusic
{
    public static class FilenameFunctions
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
    }
}
