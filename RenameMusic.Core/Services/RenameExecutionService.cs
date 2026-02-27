using RenameMusic.Models;
using System.IO;

namespace RenameMusic.Services
{
    public sealed class RenameBatchResult
    {
        public int CompletedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public int MissingCount { get; set; }
    }

    public interface IRenameExecutionService
    {
        Task<RenameBatchResult> RenameAllAsync(
            IDialogService dialogService,
            ConflictResolutionAction? defaultConflictAction = null,
            CancellationToken cancellationToken = default);
        Task<RenameBatchResult> RenameByIdsAsync(
            IEnumerable<int> ids,
            IDialogService dialogService,
            ConflictResolutionAction? defaultConflictAction = null,
            CancellationToken cancellationToken = default);
    }

    public sealed class RenameExecutionService : IRenameExecutionService
    {
        private readonly ISessionService _sessionService;

        public RenameExecutionService(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<RenameBatchResult> RenameAllAsync(
            IDialogService dialogService,
            ConflictResolutionAction? defaultConflictAction = null,
            CancellationToken cancellationToken = default)
        {
            List<AudioLibraryItem> items = await _sessionService.GetRenamableItemsAsync(cancellationToken);
            return await RenameItemsAsync(items, dialogService, defaultConflictAction, cancellationToken);
        }

        public async Task<RenameBatchResult> RenameByIdsAsync(
            IEnumerable<int> ids,
            IDialogService dialogService,
            ConflictResolutionAction? defaultConflictAction = null,
            CancellationToken cancellationToken = default)
        {
            List<AudioLibraryItem> items = await _sessionService.GetItemsByIdsAsync(ids, cancellationToken);
            return await RenameItemsAsync(items.Where(i => i.CanRename), dialogService, defaultConflictAction, cancellationToken);
        }

        private async Task<RenameBatchResult> RenameItemsAsync(
            IEnumerable<AudioLibraryItem> items,
            IDialogService dialogService,
            ConflictResolutionAction? defaultConflictAction,
            CancellationToken cancellationToken)
        {
            RenameBatchResult result = new();
            HashSet<int> idsToRemove = [];
            Dictionary<int, string> doNotRenameUpdates = [];

            ConflictResolutionAction? applyToAllAction = null;
            foreach (AudioLibraryItem item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sourcePath = item.FullPath;
                if (!File.Exists(sourcePath))
                {
                    doNotRenameUpdates[item.Id] = "File not found.";
                    result.MissingCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ProposedName))
                {
                    doNotRenameUpdates[item.Id] = "Empty proposed file name.";
                    result.FailedCount++;
                    continue;
                }

                string destinationPath = Path.Combine(item.FolderPath, $"{item.ProposedName}{item.FileExtension}");
                if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
                {
                    idsToRemove.Add(item.Id);
                    result.CompletedCount++;
                    continue;
                }

                try
                {
                    if (File.Exists(destinationPath))
                    {
                        ConflictResolutionAction action = applyToAllAction
                            ?? ResolveConflict(
                                dialogService,
                                sourcePath,
                                destinationPath,
                                defaultConflictAction,
                                ref applyToAllAction);

                        if (action == ConflictResolutionAction.Skip)
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        if (action == ConflictResolutionAction.Replace)
                        {
                            File.Delete(destinationPath);
                        }
                        else if (action == ConflictResolutionAction.RenameWithNumber)
                        {
                            destinationPath = GetIndexedDestinationPath(destinationPath);
                        }
                    }

                    File.Move(sourcePath, destinationPath);
                    idsToRemove.Add(item.Id);
                    result.CompletedCount++;
                }
                catch (Exception ex)
                {
                    doNotRenameUpdates[item.Id] = ex.Message;
                    result.FailedCount++;
                }
            }

            if (idsToRemove.Count > 0)
            {
                await _sessionService.RemoveAudiosAsync(idsToRemove, cancellationToken);
            }

            if (doNotRenameUpdates.Count > 0)
            {
                await _sessionService.MarkAsDoNotRenameAsync(doNotRenameUpdates, cancellationToken);
            }

            return result;
        }

        private static ConflictResolutionAction ResolveConflict(
            IDialogService dialogService,
            string sourcePath,
            string destinationPath,
            ConflictResolutionAction? defaultConflictAction,
            ref ConflictResolutionAction? applyToAllAction)
        {
            if (defaultConflictAction.HasValue)
            {
                applyToAllAction = defaultConflictAction.Value;
                return defaultConflictAction.Value;
            }

            ConflictDialogResult? response = dialogService.ShowConflictDialog(sourcePath, destinationPath);
            if (response is null)
            {
                return ConflictResolutionAction.Skip;
            }

            if (response.ApplyToAll)
            {
                applyToAllAction = response.Action;
            }

            return response.Action;
        }

        private static string GetIndexedDestinationPath(string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(destinationPath);
            string extension = Path.GetExtension(destinationPath);

            int index = 2;
            string candidate = Path.Combine(directory, $"{fileName} ({index}){extension}");
            while (File.Exists(candidate))
            {
                index++;
                candidate = Path.Combine(directory, $"{fileName} ({index}){extension}");
            }

            return candidate;
        }
    }
}
