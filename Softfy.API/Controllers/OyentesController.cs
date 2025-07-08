using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;

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

            if (usuario.TipoUsuario != "Oyente" && usuario.TipoUsuario != "Admin")
            {
                return Unauthorized(new { mensaje = "Este perfil no es un Oyente." });
            }

            var oyente = new
            {
                usuario.Nombre,
                usuario.Apellido,
                usuario.TipoUsuario,
                usuario.Email
            };

            return Ok(oyente);
        }

        [Authorize(Roles = "Oyente, Oyente Premium")]
        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarPerfil([FromBody] PerfilOyenteDto data)
        {
            var email = User.Identity?.Name;

            // Buscar el usuario directamente en la tabla de usuarios
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null)
                return NotFound("Oyente no encontrado.");

            // Asegurarse de que sea Oyente
            if (usuario.TipoUsuario != "Oyente" && usuario.TipoUsuario != "Oyente Premium")
                return Unauthorized(new { mensaje = "No autorizado para modificar este perfil." });

            // Actualizar campos
            usuario.Nombre = data.Nombre;
            usuario.Apellido = data.Apellido;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Perfil actualizado correctamente.",
                usuario.Nombre,
                usuario.Apellido
            });
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<PerfilOyenteDto>>> ObtenerTodosLosOyentes()
        {
            var oyentes = await _context.Users
                .Include(u => u.Suscripcion)
                    .ThenInclude(s => s.Plan)
                .Where(u => u.TipoUsuario == "Oyente" || u.TipoUsuario == "Oyente Premium")
                .ToListAsync();

            var resultado = oyentes.Select(u => new PerfilOyenteDto
            {
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                TipoUsuario = u.TipoUsuario,
                Email = u.Email // ← ¡AQUÍ estaba faltando!
            }).ToList();

            return Ok(resultado);
        }
    }
}
