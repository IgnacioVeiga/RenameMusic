using RenameMusic.Assets;
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
            string[] tags = { "<tn>", "<t>", "<a>", "<aAt>", "<At>", "<yr>" };
            string fileName = Settings.Default.DefaultTemplate;

            foreach (string tag in tags)
            {
                if (!fileName.Contains(tag)) continue;
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

        public static void RenameFile(string oldName, string newName)
        {
            if (!File.Exists(newName))
            {
                try
                {
                    // Rename it
                    File.Move(oldName, newName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Strings.EXCEPTION_MSG, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                new RepeatedFile(oldName, newName).ShowDialog();
            }
        }
    }
}
