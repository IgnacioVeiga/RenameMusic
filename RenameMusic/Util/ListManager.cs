using RenameMusic.DB;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCopies.Linq;

namespace RenameMusic.Util
{
    public static class ListManager
    {
        //public async static void FromDatabaseToListView(int pageSize)
        //{
        //    LoadingBar loadingBar = new(pageSize);
        //    loadingBar.Show();
        //    // Desde la base de datos se debe retornar una lista pequeña para cada una de las 3 listas.
        //    // Esto último debe funcionar como las páginas de un libro, con la posibilidad de elegir la
        //    // cantidad de elementos a mostrar por cada página.

        //    await Task.Run(() =>
        //    {
        //        using (MyContext context = new())
        //        {
        //        }

        //        loadingBar.Dispatcher.Invoke(() => loadingBar.UpdateProgress());
        //    });

        //    loadingBar.Close();
        //}

        public async static void AddToDatabase(string[] files)
        {
            LoadingBar loadingBar = new(files.Length);
            loadingBar.Show();

            await Task.Run(() =>
            {
                foreach (string file in files)
                {
                    // Crea la base de datos si no existe
                    _ = new MyContext().Database.EnsureCreatedAsync();

                    // Chequear si ya existen el audio o la carpeta en la base de datos
                    if (AudioAlreadyAdded(file))
                    {
                        continue;
                    }

                    int folderId;
                    string folderPath = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar;
                    if (FolderAlreadyAdded(folderPath))
                    {
                        folderId = GetFolderId(folderPath);
                    }
                    else
                    {
                        folderId = AddFolderToDB(new FolderDTO() { FolderPath = folderPath });
                    }

                    AddAudioToDB(new AudioDTO() { FilePath = file, FolderId = folderId });

                    loadingBar.Dispatcher.Invoke(() => loadingBar.UpdateProgress());
                }
            });

            loadingBar.Close();
        }

        public static int AddAudioToDB(AudioDTO audio)
        {
            using (MyContext context = new())
            {
                // Crea la base de datos si no existe
                context.Database.EnsureCreated();

                // Agrega un nuevo audio
                var resp = context.Audios.Add(audio);
                context.SaveChanges();
                return resp.Entity.Id;
            }
        }

        public static int AddFolderToDB(FolderDTO folder)
        {
            using (MyContext context = new())
            {
                // Crea la base de datos si no existe
                context.Database.EnsureCreated();

                // Agrega un nuevo audio
                var resp = context.Folders.Add(folder);
                context.SaveChanges();
                return resp.Entity.Id;
            }
        }

        public static void RemoveAudioFromDB(int id)
        {
            using (MyContext context = new())
            {
                AudioDTO audioToRemove = context.Audios.FirstPredicate(a => a.Id == id);
                context.Audios.Remove(audioToRemove);

                if (!context.Folders.AnyPredicate(f => f.Id == audioToRemove.FolderId))
                {
                    RemoveFolderFromDB(audioToRemove.FolderId);
                }

                context.SaveChanges();
            }
        }

        public static void RemoveFolderFromDB(int folderId)
        {
            using (MyContext context = new())
            {
                foreach (AudioDTO item in context.Audios.WherePredicate(a => a.FolderId == folderId))
                {
                    context.Audios.Remove(item);
                }

                context.Folders.Remove(context.Folders.FirstPredicate(a => a.Id == folderId));
                context.SaveChanges();
            }
        }

        public static bool AudioAlreadyAdded(string filepath)
        {
            using (MyContext context = new())
            {
                return context.Audios.AnyPredicate(a => a.FilePath == filepath);
            }
        }

        // ToDo: En caso de ser verdadero deberia retornar el bool y el id
        public static bool FolderAlreadyAdded(string folderpath)
        {
            using (MyContext context = new())
            {
                return context.Folders.AnyPredicate(f => f.FolderPath == folderpath);
            }
        }

        public static int GetAudioId(string filepath)
        {
            using (MyContext context = new())
            {
                return context.Audios.FirstPredicate(a => a.FilePath == filepath).Id;
            }
        }

        public static int GetFolderId(string folderpath)
        {
            using (MyContext context = new())
            {
                return context.Folders.FirstPredicate(a => a.FolderPath == folderpath).Id;
            }
        }

        public static int CountAudioItems()
        {
            using (MyContext context = new())
            {
                return context.Audios.Count();
            }
        }

        public static int CountFolderItems()
        {
            using (MyContext context = new())
            {
                return context.Folders.Count();
            }
        }
    }
}
