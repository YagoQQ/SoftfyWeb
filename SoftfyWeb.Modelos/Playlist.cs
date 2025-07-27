using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftfyWeb.Modelos
{
    public class Playlist
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        public bool EsMeGusta { get; set; } = false;

        [Required]
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        public ICollection<PlaylistCancion> PlaylistCanciones { get; set; }
    }
}
