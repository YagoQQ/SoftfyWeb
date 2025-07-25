﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
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

        [Authorize(Roles = "Artista")]
        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
                return Unauthorized(new { mensaje = "No autenticado." });

            var artista = await _context.Artistas
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);

            if (artista == null)
                return NotFound(new { mensaje = "Perfil de artista no encontrado." });

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

            return Ok(canciones); 
        }

        [HttpGet("artista/{nombre}/canciones")]
        [AllowAnonymous]
        public IActionResult ObtenerCancionesDelArtistaPorNombre(string nombre)
        {
            // Obtener el artista por su nombre
            var artista = _context.Artistas
                .FirstOrDefault(a => a.NombreArtistico == nombre);

            if (artista == null)
                return NotFound("No se encontró el artista.");

            // Obtener las canciones subidas por el artista
            var canciones = _context.Canciones
                .Where(c => c.ArtistaId == artista.Id)
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

        [HttpGet("artista/{nombre}/perfil")]
        [AllowAnonymous]
        public IActionResult ObtenerPerfilDelArtistaPorNombre(string nombre)
        {
            var artista = _context.Artistas
                .FirstOrDefault(a => a.NombreArtistico == nombre);

            if (artista == null)
                return NotFound("No se encontró el artista.");

            var perfilArtista = new
            {
                artista.Id,
                artista.NombreArtistico,
                artista.FotoUrl,
                artista.Biografia 
            };

            return Ok(perfilArtista);
        }

        [Authorize(Roles = "Artista")]
        [HttpPut("actualizar")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> ActualizarPerfil([FromForm] string nombreArtistico,[FromForm] string biografia,[FromForm] IFormFile? foto)
        {
            var email = User.Identity?.Name;

            var artista = await _context.Artistas
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Usuario.Email == email);

            if (artista == null)
                return NotFound("Artista no encontrado.");

            // Actualizar campos básicos
            artista.NombreArtistico = nombreArtistico;
            artista.Biografia = biografia;

            if (foto != null && foto.Length > 0)
            {
                var extension = Path.GetExtension(foto.FileName);
                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var ruta = Path.Combine(Directory.GetCurrentDirectory(), "FotosArtistas", nombreArchivo);

                using (var stream = new FileStream(ruta, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                artista.FotoUrl = nombreArchivo; 
            }

            await _context.SaveChangesAsync();
            return Ok("Perfil actualizado correctamente.");
        }

        [AllowAnonymous]
        [HttpGet("foto/{nombreArchivo}")]
        public IActionResult ObtenerFoto(string nombreArchivo)
        {
            var ruta = Path.Combine(Directory.GetCurrentDirectory(), "FotosArtistas", nombreArchivo);

            if (!System.IO.File.Exists(ruta))
                return NotFound("Imagen no encontrada.");

            var tipoMime = "image/jpeg";
            return PhysicalFile(ruta, tipoMime);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPerfilPorId(int id)
        {
            var artista = await _context.Artistas
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (artista == null)
                return NotFound(new { mensaje = "Artista no encontrado." });

            var perfilArtista = new
            {
                artista.Id,
                artista.NombreArtistico,
                artista.Biografia,
                artista.FotoUrl,
                UsuarioEmail = artista.Usuario.Email
            };

            return Ok(perfilArtista); 
        }
        
    }
}
