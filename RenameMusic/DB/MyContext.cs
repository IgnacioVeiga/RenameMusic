using Microsoft.EntityFrameworkCore;
using System.IO;
using WinCopies.Linq;

namespace RenameMusic.DB
{
    public class MyContext : DbContext
    {
        public DbSet<AudioDTO> Audios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            const string databaseFolder = "./Database/";
            if (!Directory.Exists(databaseFolder))
            {
                Directory.CreateDirectory(databaseFolder);
            }

            // P: Primary S: Secondary F: Folder
            optionsBuilder.UseSqlite($"Data Source={databaseFolder}database.db");
        }

        public void AddAudio(AudioDTO audio)
        {
            using (MyContext context = new())
            {
                // Crea la base de datos si no existe
                context.Database.EnsureCreated();

                // Agrega un nuevo audio
                context.Audios.Add(audio);
                context.SaveChanges();
            }
        }

        public void ModifyAudio(int id)
        {
            using (MyContext context = new())
            {

            }
        }

        public void DeleteAudio(int id)
        {
            using (MyContext context = new())
            {
                context.Audios.Remove(context.Audios.FirstPredicate(a => a.Id == id));
                context.SaveChanges();
            }
        }
    }
}
