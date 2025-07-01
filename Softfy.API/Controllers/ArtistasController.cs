using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;
using System.Threading.Tasks;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtistasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public ArtistasController(
            ApplicationDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Artistas
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ObtenerArtistas()
        {
            var artistas = _context.Artistas
                .Include(a => a.Usuario)
                .Select(a => new
                {
                    a.Id,
                    a.NombreArtistico,
                    a.Biografia,
                    a.FotoUrl,
                    UsuarioEmail = a.Usuario.Email
                })
                .ToList();

            return Ok(artistas);
        }

        // GET: api/Artistas/mi-perfil
        [Authorize(Roles = "Artista")]
        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            // 1) Obtener el Usuario ASP.NET Identity actual
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
                return Unauthorized(new { mensaje = "No autenticado." });

            // 2) Buscar la entidad Artista asociada
            var artista = await _context.Artistas
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);

            if (artista == null)
                return NotFound(new { mensaje = "Perfil de artista no encontrado." });

            // 3) Devolver solo los campos que interesan
            return Ok(new
            {
                artista.Id,
                artista.NombreArtistico,
                artista.Biografia,
                artista.FotoUrl,
                UsuarioEmail = artista.Usuario.Email
            });
        }
        [HttpGet("{id}/canciones")]
        public IActionResult ObtenerCancionesDelArtista(int id)
        {
            // Obtener las canciones subidas por el artista con el id correspondiente
            var canciones = _context.Canciones
                .Where(c => c.ArtistaId == id)
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.UrlArchivo
                })
                .ToList();

            if (!canciones.Any())
                return NotFound("No hay canciones para este artista.");

            return Ok(canciones); // Devuelve la lista de canciones del artista
        }
    }
}
