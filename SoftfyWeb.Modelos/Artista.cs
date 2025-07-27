using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftfyWeb.Modelos
{
    public class Artista
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; }  // Relación con tabla de usuarios

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Required]
        public string NombreArtistico { get; set; }

        public string? Biografia { get; set; }

        public string? FotoUrl { get; set; }
    }
}
