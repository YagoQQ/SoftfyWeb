using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Data;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using System.Security.Claims;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CancionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CancionesController(ApplicationDbContext context)
        {
            _context = context;
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

            // Validar extensión del archivo
            var allowedExtensions = new[] { ".mp3", ".wav" };
            var fileExtension = Path.GetExtension(archivoCancion.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("El tipo de archivo no es compatible. Solo se permiten archivos .mp3 y .wav.");

            // Rutas para almacenar el archivo de la canción
            var relativePath = Path.Combine("ArchivosCanciones", archivoCancion.FileName);
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            // Asegurarse de que el directorio exista
            var directory = Path.GetDirectoryName(absolutePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await archivoCancion.CopyToAsync(stream);
            }

            var cancion = new Cancion
            {
                Titulo = dto.Titulo,
                Genero = dto.Genero,
                FechaLanzamiento = DateTime.SpecifyKind(dto.FechaLanzamiento, DateTimeKind.Utc),
                UrlArchivo = relativePath,  // Ruta relativa donde se guarda el archivo
                ArtistaId = artista.Id
            };

            _context.Canciones.Add(cancion);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Canción creada correctamente" });
        }

        [HttpGet("reproducir/{nombreArchivo}")]
        [AllowAnonymous]
        public IActionResult ReproducirCancion(string nombreArchivo)
        {
            var rutaArchivo = Path.Combine(Directory.GetCurrentDirectory(), "ArchivosCanciones", nombreArchivo);

            if (!System.IO.File.Exists(rutaArchivo))
                return NotFound("El archivo no existe.");

            var fileBytes = System.IO.File.ReadAllBytes(rutaArchivo);
            return File(fileBytes, "audio/mpeg");  // Puedes cambiar el tipo MIME si es otro formato
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
                .Where(c => c.Titulo.Contains(nombre))  // Filtra por el título que contenga el nombre dado
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
    }
}
