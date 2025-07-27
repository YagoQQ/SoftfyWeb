using System.ComponentModel.DataAnnotations;

namespace SoftfyWeb.Dtos
{
    public class UsuarioRegistroDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string TipoUsuario { get; set; } // Esto es para diferenciar entre Artista y Oyente
        public string? NombreArtistico { get; set; } // Solo necesario para Artistas
        public string? Biografia { get; set; } // Solo necesario para Artistas
        public string? FotoUrl { get; set; } // Solo necesario para Artistas
    }
}
