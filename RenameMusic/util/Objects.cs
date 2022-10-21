using System;

namespace RenameMusic
{
    public class GenericFile
    {
        public string Id { get; set; } // va a usarse en un futuro para la db
        public string CarpetaId { get; set; } // identifica la carpeta
        public bool Activo { get; set; } // para filtrar
        public string NombreActual { get; set; }
        public string NuevoNombre { get; set; }
        public string Formato { get; set; } // mp3, m4a, flac, etcetera
        public double Peso { get; set; } // cuanto pesa el archivo
    }

    public class MusicFile : GenericFile
    {
        public TimeSpan Duracion { get; set; }
        public string Titulo { get; set; }
        public string Album { get; set; }
        public string Artista { get; set; }
        public string AlbumArtista { get; set; }
        public TagLib.IPicture[] Pictures { get; set; }
    }

    public class MusicFolder
    {
        public string Id { get; set; } // va a usarse en un futuro para la db
        public string CancionesId { get; set; } // identifica sus canciones
        public string Ruta { get; set; }
    }

}
