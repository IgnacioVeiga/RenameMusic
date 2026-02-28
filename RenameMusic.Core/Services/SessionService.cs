using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
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

    public interface ISessionService
    {
        Task EnsureDatabaseAsync(CancellationToken cancellationToken = default);
        Task<bool> HasSavedSessionAsync(CancellationToken cancellationToken = default);
        Task ClearSessionAsync(CancellationToken cancellationToken = default);
        Task<SessionIngestionResult> AddFilesAsync(
            IEnumerable<string> filePaths,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default);
        Task<SessionIngestionResult> AddFoldersAsync(
            IEnumerable<string> folderPaths,
            bool includeSubFolders,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default);
        Task RecalculateAllAsync(
            RenameRuleOptions options,
            CancellationToken cancellationToken = default);
        Task<SessionSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default);
        Task<List<AudioLibraryItem>> GetRenamableItemsAsync(CancellationToken cancellationToken = default);
        Task<List<AudioLibraryItem>> GetItemsByIdsAsync(
            IEnumerable<int> ids,
            CancellationToken cancellationToken = default);
        Task RemoveAudioAsync(int id, CancellationToken cancellationToken = default);
        Task RemoveAudiosAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task MarkAsDoNotRenameAsync(
            int id,
            string reason,
            CancellationToken cancellationToken = default);
        Task MarkAsDoNotRenameAsync(
            IReadOnlyDictionary<int, string> updates,
            CancellationToken cancellationToken = default);
        Task<bool> RemoveFolderAsync(int folderId, CancellationToken cancellationToken = default);
        Task RefreshAudioFromDiskAsync(
            int id,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default);
        Task<bool> TryMoveToRenameAsync(
            int id,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Persists and maintains the working session (files, folders, metadata and rename eligibility) using EF Core.
    /// </summary>
    public sealed class SessionService : ISessionService
    {
        private static readonly string[] SupportedExtensions = [".mp3", ".m4a", ".ogg", ".flac"];
        private const int SaveBatchSize = 500;
        private readonly ITemplateRuleService _templateRuleService;

        public SessionService(ITemplateRuleService templateRuleService)
        {
            _templateRuleService = templateRuleService;
        }

        public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            await EnsureSchemaAsync(context, cancellationToken);
        }

        public async Task<bool> HasSavedSessionAsync(CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            return await context.SessionAudios.AnyAsync(cancellationToken)
                || await context.SessionFolders.AnyAsync(cancellationToken);
        }

        public async Task ClearSessionAsync(CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            context.SessionAudios.RemoveRange(context.SessionAudios);
            context.SessionFolders.RemoveRange(context.SessionFolders);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Ingests explicit file paths into the session, skipping duplicates and evaluating rename eligibility in batches.
        /// </summary>
        public async Task<SessionIngestionResult> AddFilesAsync(
            IEnumerable<string> filePaths,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            await EnsureSchemaAsync(context, cancellationToken);

            HashSet<string> existingAudioPaths = new(
                await context.SessionAudios.Select(a => a.FullPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            HashSet<string> existingFolders = new(
                await context.SessionFolders.Select(f => f.FolderPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            SessionIngestionResult result = new();
            int pendingWrites = 0;
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
                    pendingWrites++;
                }

                SessionAudioEntity entity = BuildEntityFromPath(fullPath, folderPath, options, result);
                context.SessionAudios.Add(entity);
                result.AddedCount++;
                pendingWrites++;

                pendingWrites = await SaveBatchAsync(context, pendingWrites, force: false, cancellationToken);
            }

            pendingWrites = await SaveBatchAsync(context, pendingWrites, force: true, cancellationToken);
            return result;
        }

        /// <summary>
        /// Ingests one or more folders by enumerating supported audio files and applying the active rename rule.
        /// </summary>
        public async Task<SessionIngestionResult> AddFoldersAsync(
            IEnumerable<string> folderPaths,
            bool includeSubFolders,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            await EnsureSchemaAsync(context, cancellationToken);

            HashSet<string> existingAudioPaths = new(
                await context.SessionAudios.Select(a => a.FullPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            HashSet<string> existingFolders = new(
                await context.SessionFolders.Select(f => f.FolderPath).ToListAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            SessionIngestionResult result = new();
            int pendingWrites = 0;
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
                    pendingWrites++;
                }

                foreach (string filePath in EnumerateFilesSafe(normalizedFolderPath, includeSubFolders))
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
                        pendingWrites++;
                    }

                    SessionAudioEntity entity = BuildEntityFromPath(fullPath, folderPath, options, result);
                    context.SessionAudios.Add(entity);
                    result.AddedCount++;
                    pendingWrites++;

                    pendingWrites = await SaveBatchAsync(context, pendingWrites, force: false, cancellationToken);
                }
            }

            pendingWrites = await SaveBatchAsync(context, pendingWrites, force: true, cancellationToken);
            return result;
        }

        public async Task RecalculateAllAsync(
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            List<SessionAudioEntity> items = await context.SessionAudios.ToListAsync(cancellationToken);
            foreach (SessionAudioEntity item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(item.FullPath))
                {
                    item.ExistsOnDisk = false;
                    item.CanRename = false;
                    item.NotRenamableReason = NotRenamableReasonCodec.Create(NotRenamableReasonCodes.FileNotFound);
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

        /// <summary>
        /// Loads the current session split for UI tabs and reconciles missing files before mapping to view models.
        /// </summary>
        public async Task<SessionSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
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
                    bool notMarkedAsMissing = NotRenamableReasonCodec.Parse(audio.NotRenamableReason).Code != NotRenamableReasonCodes.FileNotFound;
                    if (audio.ExistsOnDisk || audio.CanRename || notMarkedAsMissing)
                    {
                        audio.ExistsOnDisk = false;
                        audio.CanRename = false;
                        audio.NotRenamableReason = NotRenamableReasonCodec.Create(NotRenamableReasonCodes.FileNotFound);
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
            await using RenameMusicDbContext context = new();
            List<SessionAudioEntity> entities = await context.SessionAudios
                .Where(a => a.CanRename)
                .OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);
            return entities.Select(MapAudioEntity).ToList();
        }

        public async Task<List<AudioLibraryItem>> GetItemsByIdsAsync(
            IEnumerable<int> ids,
            CancellationToken cancellationToken = default)
        {
            List<int> normalizedIds = ids.Distinct().ToList();
            if (normalizedIds.Count == 0)
            {
                return [];
            }

            await using RenameMusicDbContext context = new();
            List<SessionAudioEntity> entities = await context.SessionAudios
                .Where(a => normalizedIds.Contains(a.Id))
                .OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);
            return entities.Select(MapAudioEntity).ToList();
        }

        public async Task RemoveAudioAsync(int id, CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            SessionAudioEntity? entity = await context.SessionAudios.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (entity is null)
            {
                return;
            }

            context.SessionAudios.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAudiosAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            List<int> normalizedIds = ids.Distinct().ToList();
            if (normalizedIds.Count == 0)
            {
                return;
            }

            await using RenameMusicDbContext context = new();
            List<SessionAudioEntity> entities = await context.SessionAudios
                .Where(a => normalizedIds.Contains(a.Id))
                .ToListAsync(cancellationToken);
            if (entities.Count == 0)
            {
                return;
            }

            context.SessionAudios.RemoveRange(entities);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAsDoNotRenameAsync(
            int id,
            string reason,
            CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
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

        public async Task MarkAsDoNotRenameAsync(
            IReadOnlyDictionary<int, string> updates,
            CancellationToken cancellationToken = default)
        {
            if (updates.Count == 0)
            {
                return;
            }

            List<int> ids = updates.Keys.Distinct().ToList();
            await using RenameMusicDbContext context = new();
            List<SessionAudioEntity> entities = await context.SessionAudios
                .Where(a => ids.Contains(a.Id))
                .ToListAsync(cancellationToken);

            foreach (SessionAudioEntity entity in entities)
            {
                if (!updates.TryGetValue(entity.Id, out string? reason))
                {
                    continue;
                }

                entity.CanRename = false;
                entity.NotRenamableReason = reason;
                entity.ProposedName = null;
                entity.ExistsOnDisk = File.Exists(entity.FullPath);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Removes a folder and all descendant entries from the session using case-insensitive prefix matching.
        /// </summary>
        public async Task<bool> RemoveFolderAsync(int folderId, CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            SessionFolderEntity? folder = await context.SessionFolders
                .FirstOrDefaultAsync(f => f.Id == folderId, cancellationToken);
            if (folder is null)
            {
                return false;
            }

            string folderPrefixPattern = $"{EscapeLikePattern(folder.FolderPath)}%";

            List<SessionAudioEntity> audios = await context.SessionAudios
                .Where(a => EF.Functions.Like(
                    EF.Functions.Collate(a.FolderPath, "NOCASE"),
                    folderPrefixPattern,
                    "\\"))
                .ToListAsync(cancellationToken);

            if (audios.Count > 0)
            {
                context.SessionAudios.RemoveRange(audios);
            }

            List<SessionFolderEntity> folders = await context.SessionFolders
                .Where(f => EF.Functions.Like(
                    EF.Functions.Collate(f.FolderPath, "NOCASE"),
                    folderPrefixPattern,
                    "\\"))
                .ToListAsync(cancellationToken);

            if (folders.Count > 0)
            {
                context.SessionFolders.RemoveRange(folders);
            }
            else
            {
                context.SessionFolders.Remove(folder);
            }

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static string EscapeLikePattern(string rawValue)
        {
            return rawValue
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal)
                .Replace("[", "\\[", StringComparison.Ordinal);
        }

        public async Task RefreshAudioFromDiskAsync(
            int id,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await using RenameMusicDbContext context = new();
            SessionAudioEntity? entity = await context.SessionAudios
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (entity is null)
            {
                return;
            }

            if (!File.Exists(entity.FullPath))
            {
                entity.ExistsOnDisk = false;
                entity.CanRename = false;
                entity.NotRenamableReason = NotRenamableReasonCodec.Create(NotRenamableReasonCodes.FileNotFound);
                entity.ProposedName = null;
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            PopulateEntityFromFile(entity, options);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> TryMoveToRenameAsync(
            int id,
            RenameRuleOptions options,
            CancellationToken cancellationToken = default)
        {
            await RefreshAudioFromDiskAsync(id, options, cancellationToken);

            await using RenameMusicDbContext context = new();
            SessionAudioEntity? entity = await context.SessionAudios
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            return entity?.CanRename == true;
        }

        private static async Task PruneOrphanFoldersAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
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

        /// <summary>
        /// Creates base tables and indexes when needed.
        /// </summary>
        private static async Task EnsureSchemaAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
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

            await EnsureSessionAudioColumnsAsync(context, cancellationToken);
            await EnsureSessionFolderColumnsAsync(context, cancellationToken);

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

        /// <summary>
        /// Applies additive schema upgrades for persisted databases created by earlier app versions.
        /// </summary>
        private static async Task EnsureSessionAudioColumnsAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
            await TryAddColumnAsync(context, "SessionAudios", "FullPath TEXT NOT NULL DEFAULT ''", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "FolderPath TEXT NOT NULL DEFAULT ''", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "FileNameWithoutExtension TEXT NOT NULL DEFAULT ''", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "FileExtension TEXT NOT NULL DEFAULT ''", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "DurationSeconds INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "TrackNum INTEGER NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "Title TEXT NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "Album TEXT NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "AlbumArtist TEXT NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "Artist TEXT NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "Year INTEGER NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "CanRename INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "NotRenamableReason TEXT NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "ProposedName TEXT NULL", cancellationToken);
            await TryAddColumnAsync(context, "SessionAudios", "ExistsOnDisk INTEGER NOT NULL DEFAULT 1", cancellationToken);
        }

        private static async Task EnsureSessionFolderColumnsAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
            await TryAddColumnAsync(context, "SessionFolders", "FolderPath TEXT NOT NULL DEFAULT ''", cancellationToken);
        }

        private static async Task TryAddColumnAsync(
            RenameMusicDbContext context,
            string tableName,
            string columnDefinition,
            CancellationToken cancellationToken)
        {
            try
            {
                string sql = "ALTER TABLE " + tableName + " ADD COLUMN " + columnDefinition + ";";
                await context.Database.ExecuteSqlRawAsync(
                    sql,
                    cancellationToken);
            }
            catch (SqliteException ex) when (IsDuplicateColumnError(ex))
            {
                // Column already exists in this installed schema version.
            }
        }

        private static bool IsDuplicateColumnError(SqliteException ex)
        {
            return ex.SqliteErrorCode == 1
                && ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase);
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

            if (!PopulateEntityFromFile(entity, options))
            {
                result.UnreadableCount++;
            }
            return entity;
        }

        /// <summary>
        /// Reads tags with TagLib and updates session entity fields before rule evaluation.
        /// </summary>
        private bool PopulateEntityFromFile(SessionAudioEntity entity, RenameRuleOptions options)
        {
            try
            {
                using TagLib.File file = TagLib.File.Create(entity.FullPath);
                entity.DurationSeconds = (long)file.Properties.Duration.TotalSeconds;
                entity.TrackNum = file.Tag.Track > 0 ? file.Tag.Track : null;
                entity.Title = file.Tag.Title;
                entity.Album = file.Tag.Album;
                entity.AlbumArtist = file.Tag.JoinedAlbumArtists;
                entity.Artist = file.Tag.JoinedPerformers;
                entity.Year = file.Tag.Year > 0 ? file.Tag.Year : null;
                entity.ExistsOnDisk = true;
            }
            catch (Exception)
            {
                entity.CanRename = false;
                entity.NotRenamableReason = NotRenamableReasonCodec.Create(NotRenamableReasonCodes.UnreadableMetadata);
                entity.ProposedName = null;
                return false;
            }

            RuleEvaluationResult evaluation = _templateRuleService.Evaluate(entity, options);
            entity.CanRename = evaluation.CanRename;
            entity.NotRenamableReason = evaluation.Reason;
            entity.ProposedName = evaluation.ProposedName;
            return true;
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

        private static async Task<int> SaveBatchAsync(
            RenameMusicDbContext context,
            int pendingWrites,
            bool force,
            CancellationToken cancellationToken)
        {
            if (pendingWrites == 0)
            {
                return pendingWrites;
            }

            if (!force && pendingWrites < SaveBatchSize)
            {
                return pendingWrites;
            }

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return 0;
        }

        /// <summary>
        /// Traverses folders defensively, skipping inaccessible paths and reparse points to avoid loops and crashes.
        /// </summary>
        private static IEnumerable<string> EnumerateFilesSafe(string rootPath, bool includeSubFolders)
        {
            Queue<string> pendingFolders = new();
            HashSet<string> visitedFolders = new(StringComparer.OrdinalIgnoreCase);

            pendingFolders.Enqueue(rootPath);
            while (pendingFolders.Count > 0)
            {
                string currentFolder = pendingFolders.Dequeue();
                string normalizedCurrentFolder;
                try
                {
                    normalizedCurrentFolder = NormalizeFolderPath(currentFolder);
                }
                catch (Exception)
                {
                    continue;
                }

                if (!visitedFolders.Add(normalizedCurrentFolder))
                {
                    continue;
                }

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(currentFolder, "*.*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string file in files)
                {
                    yield return file;
                }

                if (!includeSubFolders)
                {
                    continue;
                }

                IEnumerable<string> subFolders;
                try
                {
                    subFolders = Directory.EnumerateDirectories(currentFolder, "*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string subFolder in subFolders)
                {
                    if (IsReparsePoint(subFolder))
                    {
                        continue;
                    }

                    pendingFolders.Enqueue(subFolder);
                }
            }
        }

        private static bool IsReparsePoint(string folderPath)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(folderPath);
                return (attributes & FileAttributes.ReparsePoint) != 0;
            }
            catch (Exception)
            {
                return true;
            }
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
