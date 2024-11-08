namespace RenameMusic.Models
{
    public class Folder(int id, string path)
    {
        public int Id = id;
        public string Path { get; set; } = path;
    }
}
