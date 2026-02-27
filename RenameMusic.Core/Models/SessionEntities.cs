namespace RenameMusic.Models
{
    public class SessionFolderEntity
    {
        public int Id { get; set; }
        public required string FolderPath { get; set; }
    }

    public class SessionAudioEntity
    {
        public int Id { get; set; }
        public required string FullPath { get; set; }
        public required string FolderPath { get; set; }
        public required string FileNameWithoutExtension { get; set; }
        public required string FileExtension { get; set; }
        public long DurationSeconds { get; set; }

        public uint? TrackNum { get; set; }
        public string? Title { get; set; }
        public string? Album { get; set; }
        public string? AlbumArtist { get; set; }
        public string? Artist { get; set; }
        public uint? Year { get; set; }

        public bool CanRename { get; set; }
        public string? NotRenamableReason { get; set; }
        public string? ProposedName { get; set; }
        public bool ExistsOnDisk { get; set; }
    }
}
