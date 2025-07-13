﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public PlaylistsController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        [HttpPost("crear")]
        public async Task<IActionResult> CrearPlaylist([FromBody] string nombre)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = new Playlist
            {
                Nombre = nombre,
                UsuarioId = usuario.Id,
                EsMeGusta = false
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Playlist creada", playlistId = playlist.Id });
        }
        [Authorize(Roles = "OyentePremium,Artista")]
        [HttpGet("mis-playlists")]
        public async Task<IActionResult> ObtenerPlaylists()
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlists = _context.Playlists
                .Where(p => p.UsuarioId == usuario.Id && !p.EsMeGusta)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    TotalCanciones = p.PlaylistCanciones.Count
                })
                .ToList();

            return Ok(playlists);
        }

        [HttpGet("{id}/canciones")]
        public async Task<IActionResult> ObtenerCanciones(int id)
        {
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistCanciones)
                    .ThenInclude(pc => pc.Cancion)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null)
                return NotFound(new { mensaje = "Playlist no encontrada." });

            var canciones = playlist.PlaylistCanciones.Select(pc => new
            {
                pc.Cancion.Id,
                pc.Cancion.Titulo,
                pc.Cancion.UrlArchivo
            }).ToList();

            return Ok(canciones);
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        [HttpPost("{playlistId}/agregar/{cancionId}")]
        public async Task<IActionResult> AgregarCancion(int playlistId, int cancionId)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = _context.Playlists
                .FirstOrDefault(p => p.Id == playlistId && p.UsuarioId == usuario.Id && !p.EsMeGusta);

            if (playlist == null)
                return NotFound("Playlist no encontrada");

            var yaExiste = _context.PlaylistCanciones.Any(pc =>
                pc.PlaylistId == playlistId && pc.CancionId == cancionId);

            if (!yaExiste)
            {
                _context.PlaylistCanciones.Add(new PlaylistCancion
                {
                    PlaylistId = playlistId,
                    CancionId = cancionId
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { mensaje = "Canción agregada a la playlist" });
        }

        [Authorize]
        [HttpDelete("{playlistId}/quitar/{cancionId}")]
        public async Task<IActionResult> QuitarCancion(int playlistId, int cancionId)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == usuario.Id);

            if (playlist == null)
                return NotFound("Playlist no encontrada");

            var relacion = await _context.PlaylistCanciones
                .FirstOrDefaultAsync(pc => pc.PlaylistId == playlistId && pc.CancionId == cancionId);

            if (relacion == null)
                return NotFound("La canción no está en esta playlist");

            _context.PlaylistCanciones.Remove(relacion);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Canción removida de la playlist" });
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        [HttpPut("{playlistId}/renombrar")]
        public async Task<IActionResult> RenombrarPlaylist(int playlistId, [FromBody] string nuevoNombre)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == usuario.Id && !p.EsMeGusta);

            if (playlist == null)
                return NotFound(new { mensaje = "Playlist no encontrada o no modificable" });

            playlist.Nombre = nuevoNombre;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Nombre de playlist actualizado", nuevoNombre });
        }

        [Authorize(Roles = "Admin,Artista,OyentePremium")]
        [HttpDelete("{playlistId}/eliminar")]
        public async Task<IActionResult> EliminarPlaylist(int playlistId)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = await _context.Playlists
                .Include(p => p.PlaylistCanciones)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return NotFound(new { mensaje = "Playlist no encontrada" });

            if (playlist.EsMeGusta)
                return BadRequest(new { mensaje = "No se puede eliminar la playlist 'Me Gusta'" });

            var esAdmin = await _userManager.IsInRoleAsync(usuario, "Admin");

            if (!esAdmin && playlist.UsuarioId != usuario.Id)
                return Forbid("No tienes permiso para eliminar esta playlist.");

            _context.PlaylistCanciones.RemoveRange(playlist.PlaylistCanciones);

            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Playlist eliminada correctamente" });
        }


        [Authorize]
        [HttpGet("me-gusta")]
        public async Task<IActionResult> ObtenerMeGusta()
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = _context.Playlists
                .FirstOrDefault(p => p.UsuarioId == usuario.Id && p.EsMeGusta);

            if (playlist == null)
                return NotFound(new { mensaje = "No tienes canciones marcadas como Me Gusta" });

            var canciones = _context.PlaylistCanciones
                .Where(pc => pc.PlaylistId == playlist.Id)
                .Select(pc => new
                {
                    pc.Cancion.Id,
                    pc.Cancion.Titulo,
                    pc.Cancion.UrlArchivo,
                    Artista = pc.Cancion.Artista.NombreArtistico
                })
                .ToList();

            return Ok(new
            {
                nombre = playlist.Nombre,
                descripcion = "Tu playlist especial de Me Gusta 💙",
                total = canciones.Count,
                canciones = canciones
            });
        }

        // Dar me gusta (agrega a playlist Me Gusta)
        //[Authorize(Roles = "OyenteFree")]
        [HttpPost("me-gusta/{cancionId}")]
        public async Task<IActionResult> DarMeGusta(int cancionId)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = _context.Playlists
                .FirstOrDefault(p => p.UsuarioId == usuario.Id && p.EsMeGusta);

            if (playlist == null)
            {
                playlist = new Playlist
                {
                    Nombre = "Canciones que te gustan",
                    UsuarioId = usuario.Id,
                    EsMeGusta = true
                };
                _context.Playlists.Add(playlist);
                await _context.SaveChangesAsync();
            }
            var yaExiste = _context.PlaylistCanciones.Any(pc =>
                pc.PlaylistId == playlist.Id && pc.CancionId == cancionId);

            if (!yaExiste)
            {
                var relacion = new PlaylistCancion
                {
                    PlaylistId = playlist.Id,
                    CancionId = cancionId
                };
                _context.PlaylistCanciones.Add(relacion);
                await _context.SaveChangesAsync();
            }

            return Ok(new { mensaje = "Canción agregada a Me Gusta" });
        }

        [Authorize]
        [HttpDelete("me-gusta/{cancionId}")]
        public async Task<IActionResult> QuitarMeGusta(int cancionId)
        {
            var usuario = await _userManager.GetUserAsync(User);

            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id && p.EsMeGusta);

            if (playlist == null)
                return NotFound(new { mensaje = "No tienes una playlist de Me Gusta" });

            var relacion = await _context.PlaylistCanciones
                .FirstOrDefaultAsync(pc => pc.PlaylistId == playlist.Id && pc.CancionId == cancionId);

            if (relacion == null)
                return NotFound(new { mensaje = "La canción no está en tu playlist de Me Gusta" });

            _context.PlaylistCanciones.Remove(relacion);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Canción removida de Me Gusta" });
        }


        [HttpGet("todas")]
        public async Task<IActionResult> ObtenerTodasLasPlaylists()
        {
            var playlists = await _context.Playlists
                .Include(p => p.Usuario)
                .Include(p => p.PlaylistCanciones)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    Propietario = p.Usuario.Email,
                    TotalCanciones = p.PlaylistCanciones.Count,
                })
                .ToListAsync();

            return Ok(playlists);
        }

        [HttpGet("todas/artistas")]
        public async Task<IActionResult> ObtenerPlaylistsDeArtistas()
        {
            var artistas = await _context.Artistas
                .Include(a => a.Usuario)
                .ToListAsync();

            var artistasIds = artistas.Select(a => a.UsuarioId).ToList();

            var playlists = await _context.Playlists
                .Include(p => p.Usuario)
                .Include(p => p.PlaylistCanciones)
                .Where(p => artistasIds.Contains(p.UsuarioId))
                .ToListAsync();

            var resultado = playlists.Select(p =>
            {
                var artista = artistas.FirstOrDefault(a => a.UsuarioId == p.UsuarioId);

                return new PlaylistDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Propietario = p.Usuario.Email,
                    NombreArtistico = artista?.NombreArtistico,
                    TotalCanciones = p.PlaylistCanciones.Count
                };
            }).ToList();

            return Ok(resultado);
        }

        [HttpGet("correo/{email}/playlists")]
        [AllowAnonymous]
        public IActionResult ObtenerPlaylistsPorCorreo(string email)
        {
            var playlists = _context.Playlists
                .Include(p => p.Usuario) 
                .Include(p => p.PlaylistCanciones)
                .Where(p => p.Usuario.Email == email && !p.EsMeGusta)
                .Select(p => new PlaylistDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    TotalCanciones = p.PlaylistCanciones.Count,
                })
                .ToList();

            return Ok(playlists);
        }

    }
}
