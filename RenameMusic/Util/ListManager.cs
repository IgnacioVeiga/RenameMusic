using RenameMusic.DB;
using RenameMusic.Entities;
using RenameMusic.Lang;
using System;
using System.IO;
using System.Threading.Tasks;
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
                // Chequear si ya existe en la base de datos
                bool alreadyAdded = primaryList.Items.As<Audio>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null
                                || secondaryList.Items.As<Audio>().FirstOrDefaultValuePredicate(f => f.FilePath == file) is not null;
                if (alreadyAdded)
                {
                    MessageBox.Show($"{file}", Strings.REPEATED_FILE, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    continue;
                }

                // Tomar el directorio, chequear si ya existe en la base de datos y crearlo si es necesario
                string directory = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar;
                Folder folder = folderList.Items.As<Folder>().FirstOrDefaultValuePredicate(f => f.Path == directory);
                if (folder is null)
                {
                    string id = Guid.NewGuid().ToString("N");
                    folder = new(id, directory);
                    folderList.Items.Add(folder);
                }

                // Crear un objeto Audio para la base de datos y añadirlo a donde corresponda
                Audio audio = new(folder.Id, file);
                if (audio.Tags is not null)
                {
                    if (string.IsNullOrEmpty(audio.NewName))
                        secondaryList.Items.Add(audio);
                    else
                        primaryList.Items.Add(audio);
                }

                // Desde la base de datos se debe retornar una lista pequeña para cada una de las 3 listas.
                // Esto último debe funcionar como las páginas de un libro, con la posibilidad de elegir la
                // cantidad de elementos a mostrar por cada página.
            }
        }

        public async static void AddItemsToList(string[] files)
        {
            LoadingBar loadingBar = new(files.Length);
            loadingBar.Show();

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    MyContext myContext = new();

                    // Chequear si ya existe en la base de datos
                    int folderId = myContext.AddFolderToList(new FolderDTO()
                    {
                        FolderPath = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar
                    });

                    // Crear un objeto Audio para la base de datos y añadirlo a donde corresponda
                    Audio audio = new(folderId.ToString(), file);
                    audio.Id = myContext.AddAudioToList(new AudioDTO() { FilePath = file }).ToString();

                    loadingBar.Dispatcher.Invoke(() => loadingBar.UpdateProgress());
                }
            });

            loadingBar.Close();
        }
    }
}
