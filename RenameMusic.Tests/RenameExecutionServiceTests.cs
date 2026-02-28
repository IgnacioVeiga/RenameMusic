using RenameMusic.Models;
using RenameMusic.Services;

namespace RenameMusic.Tests
{
    public sealed class RenameExecutionServiceTests
    {
        [Fact]
        public async Task RenameByIdsAsync_ShouldRename_WhenOnlyLetterCaseChanges()
        {
            using TempDirectoryScope scope = new();
            string sourcePath = Path.Combine(scope.Path, "song.mp3");
            await File.WriteAllTextAsync(sourcePath, "audio-data");

            string targetFileName = "SONG.mp3";
            string destinationPath = Path.Combine(scope.Path, targetFileName);

            AudioLibraryItem item = CreateAudioItem(
                id: 1,
                folderPath: scope.Path,
                originalName: "song",
                extension: ".mp3",
                proposedName: "SONG");

            FakeSessionService sessionService = new([item]);
            FakeFileDeletionService deletionService = new();
            RenameExecutionService sut = new(sessionService, deletionService);

            RenameBatchResult result = await sut.RenameByIdsAsync([1], new FakeDialogService());

            Assert.Equal(1, result.CompletedCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.Equal(0, result.MissingCount);
            Assert.Single(sessionService.RemovedIds);
            Assert.Equal(1, sessionService.RemovedIds[0]);
            Assert.Contains(targetFileName, Directory.EnumerateFiles(scope.Path).Select(Path.GetFileName), StringComparer.Ordinal);
            Assert.True(File.Exists(destinationPath));
        }

        [Fact]
        public async Task RenameByIdsAsync_ShouldUseDeletionService_WhenReplacingDestination()
        {
            using TempDirectoryScope scope = new();
            string sourcePath = Path.Combine(scope.Path, "old.mp3");
            string destinationPath = Path.Combine(scope.Path, "new.mp3");

            await File.WriteAllTextAsync(sourcePath, "new-content");
            await File.WriteAllTextAsync(destinationPath, "old-content");

            AudioLibraryItem item = CreateAudioItem(
                id: 10,
                folderPath: scope.Path,
                originalName: "old",
                extension: ".mp3",
                proposedName: "new");

            FakeSessionService sessionService = new([item]);
            FakeFileDeletionService deletionService = new();
            RenameExecutionService sut = new(sessionService, deletionService);

            RenameBatchResult result = await sut.RenameByIdsAsync(
                [10],
                new FakeDialogService(),
                ConflictResolutionAction.Replace);

            Assert.Equal(1, result.CompletedCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.Equal(0, result.MissingCount);
            Assert.Single(deletionService.DeletedFiles);
            Assert.Equal(destinationPath, deletionService.DeletedFiles[0]);
            Assert.Equal("new-content", await File.ReadAllTextAsync(destinationPath));
            Assert.Single(sessionService.RemovedIds);
            Assert.Equal(10, sessionService.RemovedIds[0]);
        }

        private static AudioLibraryItem CreateAudioItem(
            int id,
            string folderPath,
            string originalName,
            string extension,
            string proposedName)
        {
            return new AudioLibraryItem
            {
                Id = id,
                FolderPath = folderPath,
                FileNameWithoutExtension = originalName,
                FileExtension = extension,
                Duration = TimeSpan.FromSeconds(1),
                TrackNum = 1,
                Title = "Title",
                Album = "Album",
                AlbumArtist = "AlbumArtist",
                Artist = "Artist",
                Year = 2024,
                CanRename = true,
                ExistsOnDisk = true,
                ProposedName = proposedName
            };
        }

        private sealed class FakeSessionService : ISessionService
        {
            private readonly List<AudioLibraryItem> _items;

            public FakeSessionService(IEnumerable<AudioLibraryItem> items)
            {
                _items = items.ToList();
            }

            public List<int> RemovedIds { get; } = [];

            public Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task<bool> HasSavedSessionAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task ClearSessionAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task<SessionIngestionResult> AddFilesAsync(
                IEnumerable<string> filePaths,
                RenameRuleOptions options,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<SessionIngestionResult> AddFoldersAsync(
                IEnumerable<string> folderPaths,
                bool includeSubFolders,
                RenameRuleOptions options,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task RecalculateAllAsync(
                RenameRuleOptions options,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<SessionSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<List<AudioLibraryItem>> GetRenamableItemsAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(_items.Where(i => i.CanRename).ToList());

            public Task<List<AudioLibraryItem>> GetItemsByIdsAsync(
                IEnumerable<int> ids,
                CancellationToken cancellationToken = default)
            {
                HashSet<int> idSet = ids.ToHashSet();
                return Task.FromResult(_items.Where(i => idSet.Contains(i.Id)).ToList());
            }

            public Task RemoveAudioAsync(int id, CancellationToken cancellationToken = default)
            {
                RemovedIds.Add(id);
                return Task.CompletedTask;
            }

            public Task RemoveAudiosAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
            {
                RemovedIds.AddRange(ids);
                return Task.CompletedTask;
            }

            public Task MarkAsDoNotRenameAsync(int id, string reason, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task MarkAsDoNotRenameAsync(IReadOnlyDictionary<int, string> updates, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task<bool> RemoveFolderAsync(int folderId, CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task RefreshAudioFromDiskAsync(int id, RenameRuleOptions options, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task<bool> TryMoveToRenameAsync(int id, RenameRuleOptions options, CancellationToken cancellationToken = default)
                => Task.FromResult(false);
        }

        private sealed class FakeDialogService : IDialogService
        {
            public bool Confirm(string message, string title) => true;

            public void ShowInfo(string message, string title) { }

            public void ShowWarning(string message, string title) { }

            public void ShowError(string message, string title) { }

            public TemplateDialogResult? ShowTemplateDialog() => null;

            public ConflictDialogResult? ShowConflictDialog(string sourcePath, string destinationPath)
            {
                return new ConflictDialogResult
                {
                    Action = ConflictResolutionAction.Skip,
                    ApplyToAll = true
                };
            }

            public bool EditMetadata(string filePath) => false;
        }

        private sealed class FakeFileDeletionService : IFileDeletionService
        {
            public List<string> DeletedFiles { get; } = [];

            public void DeleteFile(string filePath)
            {
                DeletedFiles.Add(filePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            public void DeleteDirectory(string directoryPath)
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }

        private sealed class TempDirectoryScope : IDisposable
        {
            public TempDirectoryScope()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"renamemusic-tests-{Guid.NewGuid():N}");
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                    {
                        Directory.Delete(Path, recursive: true);
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
