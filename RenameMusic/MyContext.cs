using Microsoft.EntityFrameworkCore;
using RenameMusic.Models;
using System.IO;

namespace RenameMusic
{
    public class MyContext : DbContext
    {
        public DbSet<SessionAudioEntity> SessionAudios { get; set; }
        public DbSet<SessionFolderEntity> SessionFolders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            const string databaseFolder = "./Database/";
            if (!Directory.Exists(databaseFolder))
            {
                Directory.CreateDirectory(databaseFolder);
            }
            optionsBuilder.UseSqlite($"Data Source={databaseFolder}List.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SessionAudioEntity>()
                .HasIndex(a => a.FullPath)
                .IsUnique();

            modelBuilder.Entity<SessionAudioEntity>()
                .HasIndex(a => a.FolderPath);

            modelBuilder.Entity<SessionAudioEntity>()
                .HasIndex(a => a.FileNameWithoutExtension);

            modelBuilder.Entity<SessionAudioEntity>()
                .HasIndex(a => a.CanRename);

            modelBuilder.Entity<SessionFolderEntity>()
                .HasIndex(f => f.FolderPath)
                .IsUnique();
        }
    }
}
