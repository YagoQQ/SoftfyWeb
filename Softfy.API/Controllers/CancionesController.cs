using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Softfy.API.Services;
using SoftfyWeb.Data;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Services;
using System.Security.Claims;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CancionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AudioService _audioService;
        private readonly IConfiguration _configuration;
        private readonly Cloudinary _cloudinary;

        public CancionesController(ApplicationDbContext context, AudioService audioService, IConfiguration configuration, Cloudinary cloudinary)
        {
            _context = context;
            _audioService = audioService;
            _configuration = configuration;
            _cloudinary = cloudinary;
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


        [HttpGet("estadisticas")]
        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> Estadisticas()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var artista = _context.Artistas.FirstOrDefault(a => a.UsuarioId == usuarioId);
            if (artista == null)
                return NotFound("No se encontró el perfil del artista.");

            var canciones = _context.Canciones
                .Where(c => c.ArtistaId == artista.Id)
                .ToList();

            var stats = new List<object>();
            foreach (var cancion in canciones)
            {
                var publicId = ObtenerPublicIdDesdeUrl(cancion.UrlArchivo);

                var result = await _cloudinary.GetResourceAsync(new GetResourceParams(publicId)
                {
                    ResourceType = ResourceType.Raw
                });

                stats.Add(new
                {
                    cancion.Titulo,
                    cancion.Genero,
                    cancion.FechaLanzamiento,
                    result.Bytes,
                    result.Format,
                    result.CreatedAt
                });
            }

            return Ok(stats);
        }


        private string ObtenerPublicIdDesdeUrl(string url)
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;

            var uploadIndex = path.IndexOf("/upload/");
            if (uploadIndex < 0)
                return null;

            var afterUpload = path.Substring(uploadIndex + "/upload/".Length);

            var parts = afterUpload.Split('/', 2);
            if (parts.Length == 2 && parts[0].StartsWith("v") && long.TryParse(parts[0].Substring(1), out _))
                return parts[1];

            return afterUpload;
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

        [HttpPut("editar/{id}")]
        [Authorize(Roles = "Admin,Artista")]
        public async Task<IActionResult> EditarCancion(int id, [FromForm] CancionCrearDto song, IFormFile? nuevoArchivo)
        {
            var cancion = await _context.Canciones.FindAsync(id);

            if (cancion == null)
                return NotFound("Canción no encontrada.");

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var artista = _context.Artistas.FirstOrDefault(a => a.UsuarioId == usuarioId);

            if (!User.IsInRole("Admin") && (artista == null || cancion.ArtistaId != artista.Id))
                return Forbid("No tiene permisos para editar esta canción.");

            // Lógica para actualizar la canción
            if (nuevoArchivo != null)
            {
                var extension = Path.GetExtension(nuevoArchivo.FileName).ToLower();
                if (extension != ".mp3" && extension != ".wav")
                    return BadRequest("Solo se permiten archivos .mp3 y .wav.");

                try
                {
                    await _audioService.EliminarArchivoAsync(cancion.UrlArchivo);
                    var nuevoUpload = await _audioService.SubirAudioCloudinaryAsync(nuevoArchivo);
                    cancion.UrlArchivo = nuevoUpload.SecureUrl.ToString();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error al actualizar el archivo: {ex.Message}");
                }
            }

            cancion.Titulo = song.Titulo;
            cancion.Genero = song.Genero;
            cancion.FechaLanzamiento = DateTime.SpecifyKind(song.FechaLanzamiento, DateTimeKind.Utc);

            try
            {
                _context.Canciones.Update(cancion);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Canción actualizada correctamente", url = cancion.UrlArchivo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar cambios: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Artista")]
        public async Task<IActionResult> ObtenerCancionPorId(int id)
        {
            var cancion = await _context.Canciones.FindAsync(id);

            if (cancion == null)
                return NotFound("Canción no encontrada.");
            var respuesta = new CancionDto
            {
                Id = cancion.Id,
                Titulo = cancion.Titulo,
                Genero = cancion.Genero,
                FechaLanzamiento = cancion.FechaLanzamiento,
                UrlArchivo = cancion.UrlArchivo
            };

            return Ok(respuesta);
        }
    }
}
