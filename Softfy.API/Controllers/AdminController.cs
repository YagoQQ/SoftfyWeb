using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<Usuario> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("bloquear/{email}")]
        public async Task<IActionResult> BloquearUsuario(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { mensaje = "El email es obligatorio." });

            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            if (!usuario.LockoutEnabled)
            {
                usuario.LockoutEnabled = true;
                var updateResult = await _userManager.UpdateAsync(usuario);
                if (!updateResult.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        mensaje = "Error al habilitar el bloqueo del usuario.",
                        errores = updateResult.Errors
                    });
                }
            }
            var resultado = await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.UtcNow.AddMinutes(60));
            if (!resultado.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    mensaje = "Error al bloquear el usuario.",
                    errores = resultado.Errors
                });
            }

            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("desbloquear/{email}")]
        public async Task<IActionResult> DesbloquearUsuario(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { mensaje = "El email es obligatorio." });

            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            // Desbloquear al usuario estableciendo LockoutEnd a null
            var resultado = await _userManager.SetLockoutEndDateAsync(usuario, null);
            if (!resultado.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    mensaje = "Error al desbloquear el usuario.",
                    errores = resultado.Errors
                });
            }

            await _userManager.UpdateAsync(usuario);

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("usuarios-bloqueados")]
        public async Task<IActionResult> ObtenerUsuariosBloqueados()
        {
            var usuariosBloqueados = await _userManager.Users
                .Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow)
                .ToListAsync();

            var usuarios = usuariosBloqueados.Select(u => new
            {
                u.Nombre,
                u.Apellido,
                u.Email,
                LockoutEnd = u.LockoutEnd
            }).ToList();

            return Ok(usuarios);
        }


        // GET: api/Admin/usuarios
        [Authorize(Roles = "Admin")]
        [HttpGet("usuarios")]
        public async Task<IActionResult> ObtenerUsuarios()
        {
            var usuarios = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.TipoUsuario
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("usuario/{email}")]
        public async Task<IActionResult> EliminarUsuario(string email)
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            var resultado = await _userManager.DeleteAsync(usuario);
            if (!resultado.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    mensaje = "Error al eliminar el usuario.",
                    errores = resultado.Errors
                });
            }

            return NoContent();
        }

        [HttpGet("canciones")]
        public async Task<IActionResult> ObtenerCanciones()
        {
            var canciones = await _context.Canciones.ToListAsync();
            return Ok(canciones);
        }
        [HttpDelete("cancion/{id}")]
        public async Task<IActionResult> EliminarCancion(int id)
        {
            var cancion = await _context.Canciones.FindAsync(id);
            if (cancion == null)
                return NotFound();

            _context.Canciones.Remove(cancion);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
