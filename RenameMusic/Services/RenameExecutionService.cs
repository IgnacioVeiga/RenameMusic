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

    public sealed class RenameExecutionService
    {
        private readonly SessionService _sessionService;

        public RenameExecutionService(SessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<RenameBatchResult> RenameAllAsync(
            IDialogService dialogService,
            CancellationToken cancellationToken = default)
        {
            List<AudioLibraryItem> items = await _sessionService.GetRenamableItemsAsync(cancellationToken);
            RenameBatchResult result = new();

            ConflictResolutionAction? applyToAllAction = null;
            foreach (AudioLibraryItem item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sourcePath = item.FullPath;
                if (!File.Exists(sourcePath))
                {
                    await _sessionService.MarkAsDoNotRenameAsync(item.Id, "File not found.", cancellationToken);
                    result.MissingCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ProposedName))
                {
                    await _sessionService.MarkAsDoNotRenameAsync(item.Id, "Empty proposed file name.", cancellationToken);
                    result.FailedCount++;
                    continue;
                }

                string destinationPath = Path.Combine(item.FolderPath, $"{item.ProposedName}{item.FileExtension}");
                if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
                {
                    await _sessionService.RemoveAudioAsync(item.Id, cancellationToken);
                    result.CompletedCount++;
                    continue;
                }

                try
                {
                    if (File.Exists(destinationPath))
                    {
                        ConflictResolutionAction action = applyToAllAction
                            ?? ResolveConflict(dialogService, sourcePath, destinationPath, ref applyToAllAction);

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
                    await _sessionService.RemoveAudioAsync(item.Id, cancellationToken);
                    result.CompletedCount++;
                }
                catch (Exception ex)
                {
                    await _sessionService.MarkAsDoNotRenameAsync(item.Id, ex.Message, cancellationToken);
                    result.FailedCount++;
                }
            }

            return result;
        }

        private static ConflictResolutionAction ResolveConflict(
            IDialogService dialogService,
            string sourcePath,
            string destinationPath,
            ref ConflictResolutionAction? applyToAllAction)
        {
            ConflictDialogResult? response = dialogService.ShowConflictDialog(sourcePath, destinationPath);
            if (response is null)
            {
                return ConflictResolutionAction.Skip;
            }

            // TODO: Persist default conflict policy in settings and apply it at startup.
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
