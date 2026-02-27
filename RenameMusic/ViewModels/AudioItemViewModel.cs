using RenameMusic.Models;

namespace RenameMusic.ViewModels
{
    public sealed class AudioItemViewModel
    {
        public AudioItemViewModel(AudioLibraryItem item)
        {
            Id = item.Id;
            Folder = item.FolderPath;
            Name = item.FileNameWithoutExtension;
            Type = item.FileExtension;
            Duration = item.Duration;
            TrackNum = item.TrackNum ?? 0;
            Title = item.Title ?? string.Empty;
            Album = item.Album ?? string.Empty;
            Artist = item.Artist ?? string.Empty;
            AlbumArtist = item.AlbumArtist ?? string.Empty;
            Year = item.Year ?? 0;
            NewName = item.ProposedName ?? string.Empty;
            CanRename = item.CanRename;
            Reason = item.NotRenamableReason ?? string.Empty;
            ExistsOnDisk = item.ExistsOnDisk;
            FullPath = item.FullPath;
        }

        public int Id { get; }
        public string Folder { get; }
        public string Name { get; }
        public string Type { get; }
        public TimeSpan Duration { get; }
        public uint TrackNum { get; }
        public string Title { get; }
        public string Album { get; }
        public string Artist { get; }
        public string AlbumArtist { get; }
        public uint Year { get; }
        public string NewName { get; }
        public bool CanRename { get; }
        public string Reason { get; }
        public bool ExistsOnDisk { get; }
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
