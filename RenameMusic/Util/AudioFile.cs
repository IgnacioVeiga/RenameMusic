using RenameMusic.Lang;
using System;
using System.IO;
using System.Windows;
using TagLib;

namespace RenameMusic.Util
{
    public class AudioFile
    {
        public AudioFile(string path, string folderId)
        {
            try
            {
                TagLib.File TLF = TagLib.File.Create(path);

                Id = folderId;
                Duration = TLF.Properties.Duration;
                Tags = TLF.Tag;
                Folder = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
                FilePath = path;
            }
            catch (CorruptFileException)
            {
                Tags = null;
                MessageBox.Show($"{Strings.CORRUPT_FILE_EX_MSG}\n{path}", Strings.LOADING, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        public string Id;
        public string Folder { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public TimeSpan Duration { get; set; }

        public string NewName
        {
            get
            {
                return FilenameFunctions.GetNewName(Tags);
            }
        }

        public string FilePath
        {
            get => Folder + Name + Type;
            set
            {
                // GetFileNameWithoutExtension('C:\mydir\myfile.ext') returns 'myfile'
                Name = Path.GetFileNameWithoutExtension(value);

                // GetExtension('C:\mydir.old\myfile.ext') returns '.ext'
                Type = Path.GetExtension(value);
            }
        }

        public Tag Tags { get; set; }
    }
}
