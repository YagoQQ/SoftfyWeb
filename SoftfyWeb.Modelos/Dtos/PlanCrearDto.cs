using System.ComponentModel.DataAnnotations;

namespace SoftfyWeb.Dtos
{
    public class PlanCrearDto
    {
        [Required]
        public string Nombre { get; set; }

        [Required]
        [Range(0.01, 1000)]
        public decimal Precio { get; set; }

        [Required]
        [Range(1, 100)]
        public int MaxUsuarios { get; set; }
    }

}
