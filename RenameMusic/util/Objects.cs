using System;
using System.IO;
using TagLib;

namespace RenameMusic
{
    //public class AudioFile : Tag
    //{
    //    public override TagTypes TagTypes { get; }
    //    public override void Clear() { }

    //    public bool Active { get; set; }
    //    public TimeSpan Duration;
    //    public string Folder;

    //    public string Name { get; set; }
    //    public string NewName { get; set; }
    //    public string Type { get; set; }

    //    public string FilePath
    //    {
    //        get => Folder + Name + Type;
    //        set
    //        {
    //            // GetFileNameWithoutExtension('C:\mydir\myfile.ext') returns 'myfile'
    //            Name = Path.GetFileNameWithoutExtension(value);

    //            // GetExtension('C:\mydir.old\myfile.ext') returns '.ext'
    //            Type = Path.GetExtension(value);
    //        }
    //    }
    //}


    public class GenericFile
    {
        public string Id { get; set; } // va a usarse en un futuro para la db
        public string CarpetaId { get; set; } // identifica la carpeta
        public bool Activo { get; set; }
        public string NombreActual { get; set; }
        public string NuevoNombre { get; set; }
        public string Formato { get; set; }
        public double Peso { get; set; }
    }

    public class MusicFile : GenericFile
    {
        public TimeSpan Duracion { get; set; }
        public string Titulo { get; set; }
        public string Album { get; set; }
        public string Artista { get; set; }
        public string AlbumArtista { get; set; }
        public IPicture[] Pictures { get; set; }
    }

    public class MusicFolder
    {
        public string Id { get; set; } // va a usarse en un futuro para la db
        public string CancionesId { get; set; } // identifica sus canciones
        public string Ruta { get; set; }
    }
}
