namespace RenameMusic.DB
{
    public class AudioDTO
    {
        public int Id { get; set; }
        public int FolderId { get; set; }
        public string FileName { get; set; }
    }

    public class FolderDTO
    {
        public int Id { get; set; }
        public string FolderPath { get; set; }
    }
}
