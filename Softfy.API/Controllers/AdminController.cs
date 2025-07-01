using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Modelos;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;

        public AdminController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        // Solo admins pueden usar este endpoint
       // [Authorize(Roles = "Admin")]
        [HttpDelete("eliminar-usuarios-prueba")]
        public async Task<IActionResult> EliminarUsuariosPrueba()
        {
            var usuarios = _userManager.Users
                .ToList();

            int eliminados = 0;

            foreach (var usuario in usuarios)
            {
                var resultado = await _userManager.DeleteAsync(usuario);
                if (resultado.Succeeded)
                    eliminados++;
            }

            return Ok(new { mensaje = $"Usuarios de prueba eliminados: {eliminados}" });
        }
    }
}
