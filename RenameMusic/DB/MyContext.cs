using Microsoft.EntityFrameworkCore;
using System.IO;
using WinCopies.Linq;

namespace RenameMusic.DB
{
    public class MyContext : DbContext
    {
        // Revisar si es necesario utilizar AutoMapper
        public DbSet<AudioDTO> Audios { get; set; }
        public DbSet<FolderDTO> Folders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            const string databaseFolder = "./Database/";
            if (!Directory.Exists(databaseFolder))
            {
                Directory.CreateDirectory(databaseFolder);
            }

            // P: Primary S: Secondary F: Folder
            optionsBuilder.UseSqlite($"Data Source={databaseFolder}PSF_List.db");
        }

        public int AddAudioToList(AudioDTO audio)
        {
            using (MyContext context = new())
            {
                // Crea la base de datos si no existe
                context.Database.EnsureCreated();

                // Chequear si es repetido

                // Agrega un nuevo audio
                var resp = context.Audios.Add(audio);
                context.SaveChanges();
                return resp.Entity.Id;
            }
        }

        public int AddFolderToList(FolderDTO folder)
        {
            using (MyContext context = new())
            {
                // Crea la base de datos si no existe
                context.Database.EnsureCreated();

                // Chequear si es repetido

                // Agrega un nuevo audio
                var resp = context.Folders.Add(folder);
                context.SaveChanges();
                return resp.Entity.Id;
            }
        }

        public void RemoveAudioFromList(int id)
        {
            using (MyContext context = new())
            {
                AudioDTO audioToRemove = context.Audios.FirstPredicate(a => a.Id == id);
                context.Audios.Remove(audioToRemove);

                if (!context.Folders.AnyPredicate(f => f.Id == audioToRemove.FolderId))
                {
                    RemoveFolderFromList(audioToRemove.FolderId);
                }

                context.SaveChanges();
            }
        }

        public void RemoveFolderFromList(int folderId)
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
    }
}
