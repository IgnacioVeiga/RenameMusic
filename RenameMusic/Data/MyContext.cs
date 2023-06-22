using Microsoft.EntityFrameworkCore;
using System.IO;

namespace RenameMusic.Data
{
    public class MyContext : DbContext
    {
        public DbSet<AudioDTO> Audios { get; set; }
        public DbSet<FolderDTO> Folders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            const string databaseFolder = "./Database/";
            if (!Directory.Exists(databaseFolder))
            {
                Directory.CreateDirectory(databaseFolder);
            }
            optionsBuilder.UseSqlite($"Data Source={databaseFolder}List.db");
        }
    }
}
