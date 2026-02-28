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
        private const int LegacySchemaVersion = 1;
        private const int CurrentSchemaVersion = 2;
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
        /// Ensures the SQLite session schema is at the current version using explicit migrations.
        /// </summary>
        private static async Task EnsureSchemaAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                int version = await GetSchemaVersionAsync(context, cancellationToken);
                if (version == 0)
                {
                    version = await DetectOrInitializeSchemaVersionAsync(context, cancellationToken);
                    await SetSchemaVersionAsync(context, version, cancellationToken);
                }

                if (version > CurrentSchemaVersion)
                {
                    throw new InvalidOperationException(
                        $"Database schema version {version} is newer than supported version {CurrentSchemaVersion}.");
                }

                while (version < CurrentSchemaVersion)
                {
                    int targetVersion = version + 1;
                    await ApplyMigrationAsync(context, targetVersion, cancellationToken);
                    version = targetVersion;
                    await SetSchemaVersionAsync(context, version, cancellationToken);
                }

                await EnsureIndexesAsync(context, cancellationToken);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }

        private static async Task<int> DetectOrInitializeSchemaVersionAsync(
            RenameMusicDbContext context,
            CancellationToken cancellationToken)
        {
            bool hasAudiosTable = await TableExistsAsync(context, "SessionAudios", cancellationToken);
            bool hasFoldersTable = await TableExistsAsync(context, "SessionFolders", cancellationToken);

            if (!hasAudiosTable)
            {
                await EnsureCoreTablesAsync(context, cancellationToken);
                return CurrentSchemaVersion;
            }

            if (!hasFoldersTable)
            {
                await EnsureSessionFoldersTableAsync(context, cancellationToken);
            }

            bool hasExistsOnDiskColumn = await ColumnExistsAsync(
                context,
                "SessionAudios",
                "ExistsOnDisk",
                cancellationToken);

            if (hasExistsOnDiskColumn)
            {
                return CurrentSchemaVersion;
            }

            return LegacySchemaVersion;
        }

        private static async Task ApplyMigrationAsync(
            RenameMusicDbContext context,
            int targetVersion,
            CancellationToken cancellationToken)
        {
            switch (targetVersion)
            {
                case 2:
                    await ApplyMigrationToVersion2Async(context, cancellationToken);
                    return;
                default:
                    throw new InvalidOperationException($"No migration available for schema version {targetVersion}.");
            }
        }

        private static async Task ApplyMigrationToVersion2Async(
            RenameMusicDbContext context,
            CancellationToken cancellationToken)
        {
            await EnsureSessionFoldersTableAsync(context, cancellationToken);
            await EnsureSessionAudiosTableAsync(context, cancellationToken);

            bool hasExistsOnDiskColumn = await ColumnExistsAsync(
                context,
                "SessionAudios",
                "ExistsOnDisk",
                cancellationToken);

            if (!hasExistsOnDiskColumn)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE SessionAudios ADD COLUMN ExistsOnDisk INTEGER NOT NULL DEFAULT 1;",
                    cancellationToken);
            }
        }

        private static async Task EnsureCoreTablesAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
            await EnsureSessionFoldersTableAsync(context, cancellationToken);
            await EnsureSessionAudiosTableAsync(context, cancellationToken);
        }

        private static Task EnsureSessionFoldersTableAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
            return context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS SessionFolders (
                    Id INTEGER NOT NULL CONSTRAINT PK_SessionFolders PRIMARY KEY AUTOINCREMENT,
                    FolderPath TEXT NOT NULL
                );
                """,
                cancellationToken);
        }

        private static Task EnsureSessionAudiosTableAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
            return context.Database.ExecuteSqlRawAsync(
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
        }

        private static async Task EnsureIndexesAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
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

        private static async Task<int> GetSchemaVersionAsync(RenameMusicDbContext context, CancellationToken cancellationToken)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA user_version;";
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result is DBNull)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }

        private static Task SetSchemaVersionAsync(
            RenameMusicDbContext context,
            int version,
            CancellationToken cancellationToken)
        {
            return context.Database.ExecuteSqlRawAsync(
                "PRAGMA user_version = " + version + ";",
                cancellationToken);
        }

        private static async Task<bool> TableExistsAsync(
            RenameMusicDbContext context,
            string tableName,
            CancellationToken cancellationToken)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
            var nameParameter = command.CreateParameter();
            nameParameter.ParameterName = "$name";
            nameParameter.Value = tableName;
            command.Parameters.Add(nameParameter);

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && result is not DBNull;
        }

        private static async Task<bool> ColumnExistsAsync(
            RenameMusicDbContext context,
            string tableName,
            string columnName,
            CancellationToken cancellationToken)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(1))
                {
                    continue;
                }

                string currentColumnName = reader.GetString(1);
                if (string.Equals(currentColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
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
