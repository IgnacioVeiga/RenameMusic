using Microsoft.EntityFrameworkCore;
using RenameMusic.Models;
using System.IO;

namespace RenameMusic.Services
{
    public sealed class SessionIngestionResult
    {
        public int AddedCount { get; set; }
        public int SkippedDuplicates { get; set; }
        public int UnreadableCount { get; set; }
    }

    public sealed class SessionService
    {
        private static readonly string[] SupportedExtensions = [".mp3", ".m4a", ".ogg", ".flac"];
        private readonly TemplateRuleService _templateRuleService;

        public SessionService(TemplateRuleService templateRuleService)
        {
            _templateRuleService = templateRuleService;
        }

        public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            await EnsureSchemaAsync(context, cancellationToken);
        }

        public async Task<bool> HasSavedSessionAsync(CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            return await context.SessionAudios.AnyAsync(cancellationToken)
                || await context.SessionFolders.AnyAsync(cancellationToken);
        }

        public async Task ClearSessionAsync(CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            context.SessionAudios.RemoveRange(context.SessionAudios);
            context.SessionFolders.RemoveRange(context.SessionFolders);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SessionIngestionResult> AddFilesAsync(
            IEnumerable<string> filePaths,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            await EnsureSchemaAsync(context, cancellationToken);

            HashSet<string> existingAudioPaths = new(
                await context.SessionAudios.Select(a => a.FullPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            HashSet<string> existingFolders = new(
                await context.SessionFolders.Select(f => f.FolderPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            SessionIngestionResult result = new();
            foreach (string rawPath in filePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fullPath;
                try
                {
                    fullPath = NormalizePath(rawPath);
                }
                catch (Exception)
                {
                    result.UnreadableCount++;
                    continue;
                }

                if (!IsSupportedAudio(fullPath))
                {
                    continue;
                }

                if (!existingAudioPaths.Add(fullPath))
                {
                    result.SkippedDuplicates++;
                    continue;
                }

                string folderPath = NormalizeFolderPath(Path.GetDirectoryName(fullPath) ?? string.Empty);
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    result.UnreadableCount++;
                    continue;
                }
                if (!existingFolders.Contains(folderPath))
                {
                    context.SessionFolders.Add(new SessionFolderEntity { FolderPath = folderPath });
                    existingFolders.Add(folderPath);
                }

                SessionAudioEntity entity = BuildEntityFromPath(fullPath, folderPath, options, result);
                context.SessionAudios.Add(entity);
                result.AddedCount++;
            }

            await context.SaveChangesAsync(cancellationToken);
            return result;
        }

        public async Task<SessionIngestionResult> AddFoldersAsync(
            IEnumerable<string> folderPaths,
            bool includeSubFolders,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            await EnsureSchemaAsync(context, cancellationToken);

            HashSet<string> existingAudioPaths = new(
                await context.SessionAudios.Select(a => a.FullPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            HashSet<string> existingFolders = new(
                await context.SessionFolders.Select(f => f.FolderPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            SessionIngestionResult result = new();
            foreach (string folderRawPath in folderPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string normalizedFolderPath;
                try
                {
                    normalizedFolderPath = NormalizeFolderPath(folderRawPath);
                }
                catch (Exception)
                {
                    continue;
                }

                if (!Directory.Exists(normalizedFolderPath))
                {
                    continue;
                }

                if (!existingFolders.Contains(normalizedFolderPath))
                {
                    context.SessionFolders.Add(new SessionFolderEntity { FolderPath = normalizedFolderPath });
                    existingFolders.Add(normalizedFolderPath);
                }

                SearchOption searchOption = includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (string filePath in Directory.EnumerateFiles(normalizedFolderPath, "*.*", searchOption))
                {
                    if (!IsSupportedAudio(filePath))
                    {
                        continue;
                    }

                    string fullPath = NormalizePath(filePath);
                    if (!existingAudioPaths.Add(fullPath))
                    {
                        result.SkippedDuplicates++;
                        continue;
                    }

                    string folderPath = NormalizeFolderPath(Path.GetDirectoryName(fullPath) ?? normalizedFolderPath);
                    if (string.IsNullOrWhiteSpace(folderPath))
                    {
                        result.UnreadableCount++;
                        continue;
                    }
                    if (!existingFolders.Contains(folderPath))
                    {
                        context.SessionFolders.Add(new SessionFolderEntity { FolderPath = folderPath });
                        existingFolders.Add(folderPath);
                    }

                    SessionAudioEntity entity = BuildEntityFromPath(fullPath, folderPath, options, result);
                    context.SessionAudios.Add(entity);
                    result.AddedCount++;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            return result;
        }

        public async Task RecalculateAllAsync(
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            List<SessionAudioEntity> items = await context.SessionAudios.ToListAsync(cancellationToken);
            foreach (SessionAudioEntity item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(item.FullPath))
                {
                    item.ExistsOnDisk = false;
                    item.CanRename = false;
                    item.NotRenamableReason = "File not found.";
                    item.ProposedName = null;
                    continue;
                }

                RuleEvaluationResult evaluation = _templateRuleService.Evaluate(item, options);
                item.ExistsOnDisk = true;
                item.CanRename = evaluation.CanRename;
                item.NotRenamableReason = evaluation.Reason;
                item.ProposedName = evaluation.ProposedName;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SessionSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            await PruneOrphanFoldersAsync(context, cancellationToken);

            List<SessionAudioEntity> audioEntities = await context.SessionAudios
                .OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);
            List<SessionFolderEntity> folderEntities = await context.SessionFolders
                .OrderBy(f => f.Id)
                .ToListAsync(cancellationToken);

            int missingCount = 0;
            bool changed = false;
            foreach (SessionAudioEntity audio in audioEntities)
            {
                bool exists = File.Exists(audio.FullPath);
                if (!exists)
                {
                    missingCount++;
                    if (audio.ExistsOnDisk || audio.CanRename || audio.NotRenamableReason != "File not found.")
                    {
                        audio.ExistsOnDisk = false;
                        audio.CanRename = false;
                        audio.NotRenamableReason = "File not found.";
                        audio.ProposedName = null;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            IReadOnlyList<AudioLibraryItem> mappedAudios = audioEntities.Select(MapAudioEntity).ToList();
            IReadOnlyList<FolderLibraryItem> mappedFolders = folderEntities
                .Select(f => new FolderLibraryItem { Id = f.Id, Path = f.FolderPath })
                .ToList();

            return new SessionSnapshot
            {
                ToRename = mappedAudios.Where(a => a.CanRename).ToList(),
                DoNotRename = mappedAudios.Where(a => !a.CanRename).ToList(),
                Folders = mappedFolders,
                MissingFilesCount = missingCount
            };
        }

        public async Task<List<AudioLibraryItem>> GetRenamableItemsAsync(CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            List<SessionAudioEntity> entities = await context.SessionAudios
                .Where(a => a.CanRename)
                .OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);
            return entities.Select(MapAudioEntity).ToList();
        }

        public async Task RemoveAudioAsync(int id, CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            SessionAudioEntity? entity = await context.SessionAudios.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (entity is null)
            {
                return;
            }

            context.SessionAudios.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAsDoNotRenameAsync(
            int id,
            string reason,
            CancellationToken cancellationToken = default)
        {
            await using MyContext context = new();
            SessionAudioEntity? entity = await context.SessionAudios.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (entity is null)
            {
                return;
            }

            entity.CanRename = false;
            entity.NotRenamableReason = reason;
            entity.ProposedName = null;
            entity.ExistsOnDisk = File.Exists(entity.FullPath);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task PruneOrphanFoldersAsync(MyContext context, CancellationToken cancellationToken)
        {
            HashSet<string> activeFolderPaths = new(
                await context.SessionAudios
                .Select(a => a.FolderPath)
                .Distinct()
                .ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            List<SessionFolderEntity> folders = await context.SessionFolders.ToListAsync(cancellationToken);
            List<SessionFolderEntity> orphanFolders = folders
                .Where(f => !activeFolderPaths.Contains(f.FolderPath))
                .ToList();

            if (orphanFolders.Count == 0)
            {
                return;
            }

            context.SessionFolders.RemoveRange(orphanFolders);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureSchemaAsync(MyContext context, CancellationToken cancellationToken)
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS SessionFolders (
                    Id INTEGER NOT NULL CONSTRAINT PK_SessionFolders PRIMARY KEY AUTOINCREMENT,
                    FolderPath TEXT NOT NULL
                );
                """,
                cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS SessionAudios (
                    Id INTEGER NOT NULL CONSTRAINT PK_SessionAudios PRIMARY KEY AUTOINCREMENT,
                    FullPath TEXT NOT NULL,
                    FolderPath TEXT NOT NULL,
                    FileNameWithoutExtension TEXT NOT NULL,
                    FileExtension TEXT NOT NULL,
                    DurationSeconds INTEGER NOT NULL,
                    TrackNum INTEGER NULL,
                    Title TEXT NULL,
                    Album TEXT NULL,
                    AlbumArtist TEXT NULL,
                    Artist TEXT NULL,
                    Year INTEGER NULL,
                    CanRename INTEGER NOT NULL,
                    NotRenamableReason TEXT NULL,
                    ProposedName TEXT NULL,
                    ExistsOnDisk INTEGER NOT NULL
                );
                """,
                cancellationToken);

            await context.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_SessionAudios_FullPath ON SessionAudios (FullPath);",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_SessionAudios_FolderPath ON SessionAudios (FolderPath);",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_SessionAudios_FileNameWithoutExtension ON SessionAudios (FileNameWithoutExtension);",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_SessionAudios_CanRename ON SessionAudios (CanRename);",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_SessionFolders_FolderPath ON SessionFolders (FolderPath);",
                cancellationToken);
        }

        private SessionAudioEntity BuildEntityFromPath(
            string fullPath,
            string folderPath,
            RenameRuleOptions options,
            SessionIngestionResult result)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
            string fileExtension = Path.GetExtension(fullPath);

            SessionAudioEntity entity = new()
            {
                FullPath = fullPath,
                FolderPath = folderPath,
                FileNameWithoutExtension = fileNameWithoutExtension,
                FileExtension = fileExtension,
                ExistsOnDisk = File.Exists(fullPath)
            };

            try
            {
                using TagLib.File file = TagLib.File.Create(fullPath);
                entity.DurationSeconds = (long)file.Properties.Duration.TotalSeconds;
                entity.TrackNum = file.Tag.Track > 0 ? file.Tag.Track : null;
                entity.Title = file.Tag.Title;
                entity.Album = file.Tag.Album;
                entity.AlbumArtist = file.Tag.JoinedAlbumArtists;
                entity.Artist = file.Tag.JoinedPerformers;
                entity.Year = file.Tag.Year > 0 ? file.Tag.Year : null;
            }
            catch (Exception)
            {
                result.UnreadableCount++;
                entity.CanRename = false;
                entity.NotRenamableReason = "Unreadable metadata.";
                entity.ProposedName = null;
                return entity;
            }

            RuleEvaluationResult evaluation = _templateRuleService.Evaluate(entity, options);
            entity.CanRename = evaluation.CanRename;
            entity.NotRenamableReason = evaluation.Reason;
            entity.ProposedName = evaluation.ProposedName;
            return entity;
        }

        private static AudioLibraryItem MapAudioEntity(SessionAudioEntity entity)
        {
            return new AudioLibraryItem
            {
                Id = entity.Id,
                FolderPath = entity.FolderPath,
                FileNameWithoutExtension = entity.FileNameWithoutExtension,
                FileExtension = entity.FileExtension,
                Duration = TimeSpan.FromSeconds(entity.DurationSeconds),
                TrackNum = entity.TrackNum,
                Title = entity.Title,
                Album = entity.Album,
                AlbumArtist = entity.AlbumArtist,
                Artist = entity.Artist,
                Year = entity.Year,
                CanRename = entity.CanRename,
                NotRenamableReason = entity.NotRenamableReason,
                ProposedName = entity.ProposedName,
                ExistsOnDisk = entity.ExistsOnDisk
            };
        }

        private static bool IsSupportedAudio(string path)
        {
            string extension = Path.GetExtension(path);
            return SupportedExtensions.Any(e => extension.Equals(e, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Trim();
        }

        private static string NormalizeFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalized = Path.GetFullPath(path).Trim();
            if (!normalized.EndsWith(Path.DirectorySeparatorChar))
            {
                normalized += Path.DirectorySeparatorChar;
            }

            return normalized;
        }
    }
}
