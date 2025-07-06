using System.ComponentModel.DataAnnotations;

namespace SoftfyWeb.Modelos
{
    public class Plan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } // Individual, Familiar, Empresarial

        [Required]
        public decimal Precio { get; set; }

        [Required]
        public int MaxUsuarios { get; set; }

        public ICollection<Suscripcion>? Suscripciones { get; set; }
    }

}
