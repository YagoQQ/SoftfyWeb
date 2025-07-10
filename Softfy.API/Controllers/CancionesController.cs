using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Softfy.API.Services;
using SoftfyWeb.Data;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using System.Security.Claims;
using SoftfyWeb.Services;
using CloudinaryDotNet.Actions;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CancionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AudioService _audioService;

        public CancionesController(ApplicationDbContext context, AudioService audioService)
        {
            _context = context;
            _audioService = audioService;
        }

        [HttpPost("crear")]
        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> CrearCancion([FromForm] CancionCrearDto dto, IFormFile archivoCancion)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var artista = _context.Artistas.FirstOrDefault(a => a.UsuarioId == usuarioId);

            if (artista == null)
                return BadRequest("El usuario no tiene un perfil de artista.");

            if (archivoCancion == null || archivoCancion.Length == 0)
                return BadRequest("No se ha seleccionado un archivo para la canción.");

            var extension = Path.GetExtension(archivoCancion.FileName).ToLower();
            if (extension != ".mp3" && extension != ".wav")
                return BadRequest("Solo se permiten archivos .mp3 y .wav.");

            // Subir a Cloudinary
            RawUploadResult uploadResult;
            try
            {
                uploadResult = await _audioService.SubirAudioCloudinaryAsync(archivoCancion);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al subir a Cloudinary", detalle = ex.Message });
            }

            // Guardar en la BD
            var cancion = new Cancion
            {
                Titulo = dto.Titulo,
                Genero = dto.Genero,
                FechaLanzamiento = DateTime.SpecifyKind(dto.FechaLanzamiento, DateTimeKind.Utc),
                UrlArchivo = uploadResult.SecureUrl.ToString(),
                ArtistaId = artista.Id
            };

            _context.Canciones.Add(cancion);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Canción subida correctamente", url = cancion.UrlArchivo });
        }

        [HttpGet("mis-canciones")]
        [Authorize(Roles = "Artista")]
        public IActionResult MisCanciones()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var artista = _context.Artistas.FirstOrDefault(a => a.UsuarioId == usuarioId);
            if (artista == null)
                return NotFound(new { mensaje = "No se encontró el perfil de artista." });

            var canciones = _context.Canciones
                .Where(c => c.ArtistaId == artista.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.Genero,
                    c.FechaLanzamiento,
                    c.UrlArchivo
                })
                .ToList();

            return Ok(canciones);
        }

        [HttpGet("canciones")]
        [AllowAnonymous]
        public IActionResult ObtenerTodasLasCanciones()
        {
            var canciones = _context.Canciones
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.Genero,
                    c.FechaLanzamiento,
                    c.UrlArchivo,
                    Artista = new
                    {
                        c.Artista.NombreArtistico,
                        c.Artista.FotoUrl
                    }
                })
                .ToList();

            return Ok(canciones);
        }

        [HttpGet("canciones/nombre")]
        [AllowAnonymous]
        public IActionResult ObtenerCancionesPorNombre([FromQuery] string nombre)
        {
            var canciones = _context.Canciones
                .Where(c => c.Titulo.Contains(nombre))
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.Genero,
                    c.FechaLanzamiento,
                    c.UrlArchivo,
                    Artista = new
                    {
                        c.Artista.NombreArtistico,
                        c.Artista.FotoUrl
                    }
                })
                .ToList();

            if (!canciones.Any())
            {
                return NotFound(new { mensaje = "No se encontraron canciones con ese nombre." });
            }

            return Ok(canciones);
        }


        [HttpDelete("eliminar/{id}")]
        [Authorize(Roles = "Admin,Artista")]
        public async Task<IActionResult> EliminarCancion(int id)
        {
            var cancion = await _context.Canciones.FindAsync(id);
            if (cancion == null)
                return NotFound("Canción no encontrada.");

            try
            {
                await _audioService.EliminarArchivoAsync(cancion.UrlArchivo);
                _context.Canciones.Remove(cancion);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Canción eliminada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar: {ex.Message}");
            }
        }
    }
}
