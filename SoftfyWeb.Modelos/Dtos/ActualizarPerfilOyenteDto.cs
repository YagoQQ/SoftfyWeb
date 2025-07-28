using System.ComponentModel.DataAnnotations;

namespace SoftfyWeb.Modelos.Dtos
{
    public class ActualizarPerfilOyenteDto
    {
        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido { get; set; }
    }
}
