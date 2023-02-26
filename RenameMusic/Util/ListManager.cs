using RenameMusic.Lang;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinCopies.Linq;

namespace RenameMusic.Util
{
    public static class ListManager
    {
        public static void AddFilesToListView(string[] files, ListView primaryList, ListView secondaryList, ListView folderList)
        {
            foreach (string file in files)
            {
                bool alreadyAdded = primaryList.Items.As<AudioFile>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null
                                || secondaryList.Items.As<AudioFile>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null;

                if (alreadyAdded)
                {
                    MessageBox.Show($"{file}", Strings.REPEATED_FILE, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    continue;
                }

                string folderPath = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar;
                Folder folder = folderList.Items.As<Folder>().FirstOrDefaultValuePredicate(f => f.Path == folderPath);
                if (folder is null)
                {
                    string id = Guid.NewGuid().ToString("N");
                    folder = new(id, folderPath);
                    folderList.Items.Add(folder);
                }

                AudioFile audiofile = new(folder.Id, file);
                if (audiofile.Tags is not null)
                {
                    if (string.IsNullOrEmpty(audiofile.NewName))
                        secondaryList.Items.Add(audiofile);
                    else
                        primaryList.Items.Add(audiofile);
                }
            }
        }
    }
}
