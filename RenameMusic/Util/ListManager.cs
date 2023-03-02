using RenameMusic.Entities;
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
                bool alreadyAdded = primaryList.Items.As<Audio>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null
                                || secondaryList.Items.As<Audio>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null;

                if (alreadyAdded)
                {
                    MessageBox.Show($"{file}", Strings.REPEATED_FILE, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    continue;
                }

                string directory = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar;
                Folder folder = folderList.Items.As<Folder>().FirstOrDefaultValuePredicate(f => f.Path == directory);
                if (folder is null)
                {
                    string id = Guid.NewGuid().ToString("N");
                    folder = new(id, directory);
                    folderList.Items.Add(folder);
                }

                Audio audio = new(folder.Id, file);
                if (audio.Tags is not null)
                {
                    if (string.IsNullOrEmpty(audio.NewName))
                        secondaryList.Items.Add(audio);
                    else
                        primaryList.Items.Add(audio);
                }
            }
        }
    }
}
