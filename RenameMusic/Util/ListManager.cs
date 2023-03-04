using RenameMusic.DB;
using System.IO;
using System.Threading.Tasks;

namespace RenameMusic.Util
{
    public static class ListManager
    {
        public static void AddItemsToListView()
        {
            //Audio audio = new(folder.Id, file);
            //if (audio.Tags is not null)
            //{
            //    if (string.IsNullOrEmpty(audio.NewName))
            //        secondaryList.Items.Add(audio);
            //    else
            //        primaryList.Items.Add(audio);
            //}

            // Desde la base de datos se debe retornar una lista pequeña para cada una de las 3 listas.
            // Esto último debe funcionar como las páginas de un libro, con la posibilidad de elegir la
            // cantidad de elementos a mostrar por cada página.
        }

        public async static void AddToDatabase(string[] files)
        {
            LoadingBar loadingBar = new(files.Length);
            loadingBar.Show();
            MyContext myContext = new();

            await Task.Run(() =>
            {
                foreach (string file in files)
                {
                    // Chequear si ya existen el audio o la carpeta en la base de datos
                    if (myContext.AudioAlreadyAdded(file))
                    {
                        continue;
                    }

                    int folderId;
                    string folderPath = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar;
                    if (myContext.FolderAlreadyAdded(folderPath))
                    {
                        folderId = myContext.GetFolderId(folderPath);
                    }
                    else
                    {
                        folderId = myContext.AddFolderToDB(new FolderDTO() { FolderPath = folderPath });
                    }

                    myContext.AddAudioToDB(new AudioDTO() { FilePath = file, FolderId = folderId });

                    loadingBar.Dispatcher.Invoke(() => loadingBar.UpdateProgress());
                }
            });

            await myContext.DisposeAsync();
            loadingBar.Close();
        }
    }
}
