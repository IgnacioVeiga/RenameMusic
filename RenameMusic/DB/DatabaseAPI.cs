using RenameMusic.Entities;
using RenameMusic.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinCopies.Linq;

namespace RenameMusic.DB
{
    public static class DatabaseAPI
    {
        public static void BeforeAddToDB(string[] files)
        {
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
        public static void ClearDatabase()
        {
            using MyContext context = new();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        #region AddToDB
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
        #endregion AddtoDB

        #region RemoveFromDB
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
        #endregion RemoveFromDB

        #region AlreadyAdded
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
        #endregion AlreadyAdded

        #region GetID
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
        #endregion GetID

        #region GetByPage
        public static List<Audio> GetPageOfAudios(int pageSize, int pageNumber, bool canRename)
        {
            using MyContext context = new();
            List<AudioDTO> audios = context.Audios.WherePredicate(a => a.Rename == canRename)
                .OrderBy(p => p.Id).Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToList();

            // ToDo: Buscar otra forma más facil. Se puede hacer con AutoMapper?
            List<Audio> list = new();
            foreach (AudioDTO audio in audios)
            {
                Audio item = new(
                    audio.FolderId,
                    new MyContext().Folders.First(f => f.Id == audio.FolderId).FolderPath + audio.FileName
                    );

                if (item.Tags != null)
                {
                    list.Add(item);
                }
            }
            return list;
        }
        public static List<Folder> GetPageOfFolders(int pageSize, int pageNumber)
        {
            using MyContext context = new();
            List<FolderDTO> folders = context.Folders
                .OrderBy(p => p.Id).Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToList();

            // ToDo: Buscar otra forma más facil. Se puede hacer con AutoMapper?
            List<Folder> list = new();
            foreach (FolderDTO folder in folders)
            {
                list.Add(new Folder(folder.Id, folder.FolderPath));
            }
            return list;
        }
        #endregion GetByPage

        #region CountItems
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
        #endregion CountItems
    }
}
