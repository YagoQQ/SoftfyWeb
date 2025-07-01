using Microsoft.AspNetCore.Identity;

namespace SoftfyWeb.Modelos
{
    public class Usuario : IdentityUser
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string TipoUsuario { get; set; } // Admin, OyenteFree, etc.
        public Suscripcion? Suscripcion { get; set; }

    }
}
