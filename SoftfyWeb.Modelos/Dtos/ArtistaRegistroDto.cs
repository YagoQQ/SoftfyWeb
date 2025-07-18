using System.ComponentModel.DataAnnotations;

namespace SoftfyWeb.Dtos
{
    public class ArtistaRegistroDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }

        [Required(ErrorMessage = "El nombre artístico es obligatorio.")]
        public string NombreArtistico { get; set; }
        public string? Biografia { get; set; }
        public string? FotoUrl { get; set; }
    }
}
