namespace RenameMusic.Entities
{
    public class Folder
    {
        public Folder(string id, string path)
        {
            Id = id;
            Path = path;
        }

        public string Id;
        public string Path { get; set; }
    }
}
