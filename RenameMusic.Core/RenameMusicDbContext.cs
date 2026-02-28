using Microsoft.EntityFrameworkCore;
using RenameMusic.Models;
using System.IO;

namespace RenameMusic
{
    /// <summary>
    /// EF Core context for persisted session state (audio items and tracked folders).
    /// </summary>
    public sealed class RenameMusicDbContext : DbContext
    {
        public DbSet<SessionAudioEntity> SessionAudios { get; set; }
        public DbSet<SessionFolderEntity> SessionFolders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? overridePath = Environment.GetEnvironmentVariable("RENAMEMUSIC_DB_PATH");
            string databasePath = string.IsNullOrWhiteSpace(overridePath)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RenameMusic",
                    "Database",
                    "List.db")
                : Path.GetFullPath(overridePath);

            string databaseFolder = Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory;
            Directory.CreateDirectory(databaseFolder);

            optionsBuilder.UseSqlite($"Data Source={databasePath}");
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
