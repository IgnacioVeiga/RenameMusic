using Microsoft.Data.Sqlite;
using RenameMusic.Models;
using RenameMusic.Services;

namespace RenameMusic.Tests
{
    public sealed class SessionServiceSchemaMigrationTests
    {
        [Fact]
        public async Task EnsureDatabaseAsync_ShouldMigrateLegacySchema_ToCurrentVersion()
        {
            using TempDbPathScope dbScope = new();

            await CreateLegacySchemaAsync(dbScope.DatabasePath);

            SessionService sut = new(new TemplateRuleService());

            await sut.EnsureDatabaseAsync();
            int userVersion = await ReadUserVersionAsync(dbScope.DatabasePath);
            SessionSnapshot snapshot = await sut.LoadSnapshotAsync();

            Assert.Equal(2, userVersion);
            Assert.NotNull(snapshot);
            Assert.Empty(snapshot.ToRename);
            Assert.Single(snapshot.DoNotRename);
            Assert.Equal(1, snapshot.MissingFilesCount);
        }

        private static async Task CreateLegacySchemaAsync(string dbPath)
        {
            await using SqliteConnection connection = new($"Data Source={dbPath}");
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE SessionFolders (
                    Id INTEGER NOT NULL CONSTRAINT PK_SessionFolders PRIMARY KEY AUTOINCREMENT,
                    FolderPath TEXT NOT NULL
                );

                CREATE TABLE SessionAudios (
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
                    ProposedName TEXT NULL
                );

                INSERT INTO SessionFolders (FolderPath) VALUES ('C:\Music\');
                INSERT INTO SessionAudios (
                    FullPath,
                    FolderPath,
                    FileNameWithoutExtension,
                    FileExtension,
                    DurationSeconds,
                    CanRename,
                    NotRenamableReason,
                    ProposedName)
                VALUES (
                    'C:\Music\missing.mp3',
                    'C:\Music\',
                    'missing',
                    '.mp3',
                    120,
                    1,
                    NULL,
                    'missing');
                """;

            await command.ExecuteNonQueryAsync();
        }

        private static async Task<int> ReadUserVersionAsync(string dbPath)
        {
            await using SqliteConnection connection = new($"Data Source={dbPath}");
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version;";
            object? result = await command.ExecuteScalarAsync();

            return result is null || result is DBNull
                ? 0
                : Convert.ToInt32(result);
        }

        private sealed class TempDbPathScope : IDisposable
        {
            private readonly string _previousDbPath;

            public TempDbPathScope()
            {
                DatabasePath = Path.Combine(
                    Path.GetTempPath(),
                    $"renamemusic-schema-upgrade-{Guid.NewGuid():N}.db");

                _previousDbPath = Environment.GetEnvironmentVariable("RENAMEMUSIC_DB_PATH") ?? string.Empty;
                Environment.SetEnvironmentVariable("RENAMEMUSIC_DB_PATH", DatabasePath);
            }

            public string DatabasePath { get; }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(
                    "RENAMEMUSIC_DB_PATH",
                    string.IsNullOrWhiteSpace(_previousDbPath) ? null : _previousDbPath);

                try
                {
                    if (File.Exists(DatabasePath))
                    {
                        File.Delete(DatabasePath);
                    }
                }
                catch
                {
                    // Best-effort test cleanup.
                }
            }
        }
    }
}
