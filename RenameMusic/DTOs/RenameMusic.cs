namespace RenameMusic.DTOs
{
    public class CancionDTO
    {
        public string IdCarpeta { get; set; } // identifica la carpeta
        public bool Activo { get; set; } // para filtrar
        public string NombreActual { get; set; } // del archivo
        public string NuevoNombre { get; set; } // del archivo
        public string Formato { get; set; } // mp3, m4a, flac
        public string Titulo { get; set; }
        public string Album { get; set; }
        public string Artista { get; set; }
        public string AlbumArtista { get; set; }
    }

    public class CarpetaDTO
    {
        public string IdCanciones { get; set; } // identifica sus canciones
        public string Ruta { get; set; }
    }
}
