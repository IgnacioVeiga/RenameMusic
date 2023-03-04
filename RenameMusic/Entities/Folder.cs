namespace RenameMusic.Entities
{
    public class Folder
    {
        public Folder(int id, string path)
        {
            Id = id;
            Path = path;
        }

        public int Id;
        public string Path { get; set; }
    }
}
