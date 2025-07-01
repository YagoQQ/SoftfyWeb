using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftfyWeb.Modelos
{
    public class Suscripcion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioPrincipalId { get; set; }

        [ForeignKey("UsuarioPrincipalId")]
        public Usuario UsuarioPrincipal { get; set; }

        [Required]
        public int PlanId { get; set; }

        [ForeignKey("PlanId")]
        public Plan Plan { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public ICollection<MiembroSuscripcion> Miembros { get; set; }
    }

}
