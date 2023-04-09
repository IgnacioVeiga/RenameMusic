using RenameMusic.Properties;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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

                AudioDTO audio = new()
                {
                    FileName = Path.GetFileName(file),
                    FolderId = folderId
                };

                // ToDo: crear función que me defina si debe o no renomrarse
                try
                {
                    TagLib.Tag tags = TagLib.File.Create(file).Tag;
                    int tagsRequiredCount = 0, tagsNotEmptyCount = 0;

                    tagsRequiredCount = Settings.Default.TitleRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
                    tagsRequiredCount = Settings.Default.AlbumRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
                    tagsRequiredCount = Settings.Default.AlbumArtistRequired ? tagsRequiredCount + 1 : tagsRequiredCount;
                    tagsRequiredCount = Settings.Default.ArtistRequired ? tagsRequiredCount + 1 : tagsRequiredCount;

                    if (Settings.Default.TitleRequired && !string.IsNullOrWhiteSpace(tags.Title))
                    {
                        tagsNotEmptyCount++;
                    }

                    if (Settings.Default.AlbumRequired && !string.IsNullOrWhiteSpace(tags.Album))
                    {
                        tagsNotEmptyCount++;
                    }

                    if (Settings.Default.AlbumArtistRequired && !string.IsNullOrWhiteSpace(tags.JoinedAlbumArtists))
                    {
                        tagsNotEmptyCount++;
                    }

                    if (Settings.Default.ArtistRequired && !string.IsNullOrWhiteSpace(tags.JoinedPerformers))
                    {
                        tagsNotEmptyCount++;
                    }

                    audio.Rename = tagsRequiredCount == tagsNotEmptyCount;
                }
                catch (Exception)
                {
                    // Se omite archivos corruptos y/o que no tengan "tags"
                    continue;
                }
                AddAudioToDB(audio);
            }
        }

        public static int AddAudioToDB(AudioDTO audio)
        {
            using MyContext context = new();
            var resp = context.Audios.Add(audio);
            context.SaveChanges();
            return resp.Entity.Id;
        }

        public static int AddFolderToDB(FolderDTO folder)
        {
            using MyContext context = new();
            var resp = context.Folders.Add(folder);
            context.SaveChanges();
            return resp.Entity.Id;
        }

        public static void RemoveAudioFromDB(int id)
        {
            using MyContext context = new();
            AudioDTO audioToRemove = context.Audios.FirstPredicate(a => a.Id == id);
            context.Audios.Remove(audioToRemove);

            if (!context.Folders.AnyPredicate(f => f.Id == audioToRemove.FolderId))
            {
                RemoveFolderFromDB(audioToRemove.FolderId);
            }

            context.SaveChanges();
        }

        public static void RemoveFolderFromDB(int folderId)
        {
            using MyContext context = new();
            foreach (AudioDTO item in context.Audios.WherePredicate(a => a.FolderId == folderId))
            {
                context.Audios.Remove(item);
            }

            context.Folders.Remove(context.Folders.FirstPredicate(a => a.Id == folderId));
            context.SaveChanges();
        }

        public static bool AudioAlreadyAdded(string filepath)
        {
            using MyContext context = new();
            return context.Audios.AnyPredicate(a => a.FileName == Path.GetFileName(filepath));
        }

        public static bool FolderAlreadyAdded(string folderpath)
        {
            using MyContext context = new();
            return context.Folders.AnyPredicate(f => f.FolderPath == folderpath);
        }

        public static int GetAudioId(string filepath)
        {
            using MyContext context = new();
            return context.Audios.FirstPredicate(a => a.FileName == Path.GetFileName(filepath)).Id;
        }

        public static int GetFolderId(string folderpath)
        {
            using MyContext context = new();
            return context.Folders.FirstPredicate(a => a.FolderPath == folderpath).Id;
        }

        public static int CountAudioItems(bool canRename)
        {
            using MyContext context = new();
            return context.Audios.Count(a => a.Rename == canRename);
        }

        public static int CountFolderItems()
        {
            using MyContext context = new();
            return context.Folders.Count();
        }

        public static void ClearDatabase()
        {
            using MyContext context = new();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
    }
}
