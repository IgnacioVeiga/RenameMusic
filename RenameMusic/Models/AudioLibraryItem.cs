using System.IO;

namespace RenameMusic.Models
{
    public sealed class AudioLibraryItem
    {
        public int Id { get; init; }
        public required string FolderPath { get; init; }
        public required string FileNameWithoutExtension { get; init; }
        public required string FileExtension { get; init; }
        public required TimeSpan Duration { get; init; }

        public uint? TrackNum { get; init; }
        public string? Title { get; init; }
        public string? Album { get; init; }
        public string? AlbumArtist { get; init; }
        public string? Artist { get; init; }
        public uint? Year { get; init; }

        public bool CanRename { get; init; }
        public string? NotRenamableReason { get; init; }
        public string? ProposedName { get; init; }
        public bool ExistsOnDisk { get; init; }

        public string FullPath => Path.Combine(FolderPath, $"{FileNameWithoutExtension}{FileExtension}");
        public string DisplayName => $"{FileNameWithoutExtension}{FileExtension}";
    }

    public sealed class FolderLibraryItem
    {
        public int Id { get; init; }
        public required string Path { get; init; }
    }

    public sealed class SessionSnapshot
    {
        public required IReadOnlyList<AudioLibraryItem> ToRename { get; init; }
        public required IReadOnlyList<AudioLibraryItem> DoNotRename { get; init; }
        public required IReadOnlyList<FolderLibraryItem> Folders { get; init; }
        public int MissingFilesCount { get; init; }
    }
}
