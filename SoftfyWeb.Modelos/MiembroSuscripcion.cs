using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace SoftfyWeb.Modelos
{
    public class MiembroSuscripcion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SuscripcionId { get; set; }

        [ForeignKey("SuscripcionId")]
        public Suscripcion Suscripcion { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        public DateTime FechaAgregado { get; set; } = DateTime.UtcNow;
    }

}
