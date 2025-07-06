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
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<Usuario> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpDelete("usuario/{email}")]
        public async Task<IActionResult> EliminarUsuario(string email)
        {
            // Buscar el usuario por Email usando UserManager
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

            return NoContent(); // 204 OK sin cuerpo
        }
    }
}
