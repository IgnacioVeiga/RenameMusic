namespace RenameMusic.Models
{
    public class AudioDTO
    {
        public int Id { get; set; }
        public int FolderId { get; set; }
        public required string FileName { get; set; }
        public bool Rename { get; set; }
    }

    public class FolderDTO
    {
        public int Id { get; set; }
        public required string FolderPath { get; set; }
    }
}
