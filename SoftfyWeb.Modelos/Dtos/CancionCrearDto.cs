using System.ComponentModel.DataAnnotations;

namespace SoftfyWeb.Dtos
{
    public class CancionCrearDto
    {
        [Required]
        public string Titulo { get; set; }

        public string? Genero { get; set; }

        [Required]
        public DateTime FechaLanzamiento { get; set; }

        public TimeSpan? Duracion { get; set; }

    }

}
