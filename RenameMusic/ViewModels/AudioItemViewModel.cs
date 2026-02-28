using RenameMusic.Models;
using RenameMusic.Services;

namespace RenameMusic.ViewModels
{
    public sealed class AudioItemViewModel
    {
        public AudioItemViewModel(AudioLibraryItem item)
        {
            Id = item.Id;
            Name = item.FileNameWithoutExtension;
            Type = item.FileExtension;
            Duration = item.Duration;
            TrackNum = item.TrackNum ?? 0;
            Title = item.Title ?? string.Empty;
            Album = item.Album ?? string.Empty;
            Artist = item.Artist ?? string.Empty;
            AlbumArtist = item.AlbumArtist ?? string.Empty;
            NewName = item.ProposedName ?? string.Empty;
            Reason = NotRenamableReasonTextService.ToDisplayText(item.NotRenamableReason);
            FullPath = item.FullPath;
        }

        public int Id { get; }
        public string Name { get; }
        public string Type { get; }
        public TimeSpan Duration { get; }
        public uint TrackNum { get; }
        public string Title { get; }
        public string Album { get; }
        public string Artist { get; }
        public string AlbumArtist { get; }
        public string NewName { get; }
        public string Reason { get; }
        public string FullPath { get; }
    }

    public sealed class FolderItemViewModel
    {
        public FolderItemViewModel(FolderLibraryItem item)
        {
            Id = item.Id;
            Path = item.Path;
        }

        public int Id { get; }
        public string Path { get; }
    }
}
