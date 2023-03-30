using System.IO;
using System.Linq;
using WinCopies.Linq;

namespace RenameMusic.DB
{
    public static class DatabaseAPI
    {
        public static void AddToDatabase(string[] files)
        {
            // ToDo: Utilizar otro hilo para esta tarea!
            foreach (string file in files)
            {
                if (AudioAlreadyAdded(file)) continue;

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

                AddAudioToDB(new AudioDTO() { FileName = Path.GetFileName(file), FolderId = folderId });
            }
        }

        public static int AddAudioToDB(AudioDTO audio)
        {
            using (MyContext context = new())
            {
                var resp = context.Audios.Add(audio);
                context.SaveChanges();
                return resp.Entity.Id;
            }
        }

        public static int AddFolderToDB(FolderDTO folder)
        {
            using (MyContext context = new())
            {
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
                return context.Audios.AnyPredicate(a => a.FileName == Path.GetFileName(filepath));
            }
        }

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
                return context.Audios.FirstPredicate(a => a.FileName == Path.GetFileName(filepath)).Id;
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

        public static void ClearDatabase()
        {
            using (MyContext context = new())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }
    }
}
