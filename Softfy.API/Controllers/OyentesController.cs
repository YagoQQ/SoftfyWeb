using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;

namespace Softfy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OyentesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public OyentesController(
            ApplicationDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            // 1) Obtener el Usuario ASP.NET Identity actual
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
                return Unauthorized(new { mensaje = "No autenticado." });

            // 2) Buscar la entidad Artista asociada
            var oyente = await _context.Users
                .FirstOrDefaultAsync(a => a.Id == usuario.Id);

            if (oyente == null)
                return NotFound(new { mensaje = "Perfil de artista no encontrado." });

            // 3) Devolver solo los campos que interesan
            return Ok(new
            {
                usuario.Nombre,
                usuario.Apellido,
                usuario.TipoUsuario
            });
        }
    }
}
