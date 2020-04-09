using System;

namespace RenameMusic.DTOs
{
    public class CancionDTO
    {
        public int Id { get; set; } // va a usarse en un futuro para la db
        public string CarpetaId { get; set; } // identifica la carpeta
        public bool Activo { get; set; } // para filtrar
        public string NombreActual { get; set; } // del archivo
        public string NuevoNombre { get; set; } // del archivo
        public string Formato { get; set; } // mp3, m4a, flac
        public TimeSpan Duracion { get; set; }
        public string Titulo { get; set; }
        public string Album { get; set; }
        public string Artista { get; set; }
        public string AlbumArtista { get; set; }
    }

    public class CarpetaDTO
    {
        public int Id { get; set; } // va a usarse en un futuro para la db
        public string CancionesId { get; set; } // identifica sus canciones
        public string Ruta { get; set; }
    }
}
