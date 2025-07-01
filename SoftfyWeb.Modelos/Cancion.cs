using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftfyWeb.Modelos
{
    public class Cancion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Titulo { get; set; }

        public string? Genero { get; set; }

        public DateTime FechaLanzamiento { get; set; }
        public TimeSpan? Duracion { get; set; }

        public string? UrlArchivo { get; set; }

        [Required]
        public int ArtistaId { get; set; }

        [ForeignKey("ArtistaId")]
        public Artista Artista { get; set; }
    }
}
