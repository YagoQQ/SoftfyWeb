using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Modelos;
using SoftfyWeb.Data;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<Usuario> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Método para bloquear un usuario
        [HttpPost("bloquear/{email}")]
        public async Task<IActionResult> BloquearUsuario(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { mensaje = "El email es obligatorio." });

            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            // Habilitar Lockout si no está habilitado
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
            var resultado = await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.UtcNow.AddHours(5));
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


        [HttpGet("usuarios-bloqueados")]
        public async Task<IActionResult> ObtenerUsuariosBloqueados()
        {
            var usuariosBloqueados = await _userManager.Users
                .Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow)
                .ToListAsync();

            var usuarios = usuariosBloqueados.Select(u => new
            {
                u.UserName,
                u.Email,
                LockoutEnd = u.LockoutEnd
            }).ToList();

            return Ok(usuarios);
        }

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
    }
}
