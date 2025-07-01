using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace SoftfyWeb.Controllers
{
    [Authorize]
    public class VistasPlaylistsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasPlaylistsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private ErrorViewModel CrearErrorModel()
        {
            string id = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return new ErrorViewModel { RequestId = id };
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public async Task<IActionResult> Index()
        {
            var client = ObtenerClienteConToken();
            var resp = await client.GetAsync("playlists/mis-playlists");
            if (!resp.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var raw = await resp.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var listas = JsonSerializer.Deserialize<List<PlaylistDto>>(raw, opts);
            return View(listas);
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError("", "El nombre es requerido");
                return View();
            }

            var client = ObtenerClienteConToken();
            var resp = await client.PostAsJsonAsync("playlists/crear", nombre);
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var err = await resp.Content.ReadAsStringAsync();
            ModelState.AddModelError("", err);
            return View();
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public async Task<IActionResult> Detalle(int id)
        {
            var client = ObtenerClienteConToken();
            // Obtener la información de la playlist
            var resp = await client.GetAsync($"playlists/{id}/canciones");
            if (!resp.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var raw = await resp.Content.ReadAsStringAsync();
            var canciones = JsonSerializer.Deserialize<List<PlaylistCancionDto>>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Crear las URL completas para los archivos de audio
            foreach (var cancion in canciones)
            {
                // Asegurarse de que solo tenemos el nombre del archivo (por ejemplo: "for.mp3")
                var nombreArchivo = Path.GetFileName(cancion.UrlArchivo);  // Extraer solo el nombre del archivo
                cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
            }

            ViewBag.PlaylistId = id;
            return View(canciones);
        }


        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Renombrar(int id, string nuevoNombre)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.PutAsJsonAsync($"playlists/{id}/renombrar", nuevoNombre);
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var err = await resp.Content.ReadAsStringAsync();
            TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.DeleteAsync($"playlists/{id}/eliminar");
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            return View("Error", CrearErrorModel());
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> QuitarCancion(int playlistId, int cancionId)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.DeleteAsync($"playlists/{playlistId}/quitar/{cancionId}");
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Detalle), new { id = playlistId });

            return View("Error", CrearErrorModel());
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarCancion(int playlistId, int cancionId)
        {
            // Obtener el ID del artista logueado desde el token (esto se puede hacer por Claims)
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (usuarioId == null)
            {
                // Si no hay ID de usuario, redirigir a error o a login
                return RedirectToAction("Login", "Account");
            }

            // Configurar el cliente HTTP con el token de autenticación
            var client = ObtenerClienteConToken();

            // Hacer la solicitud para obtener las canciones del artista
            var respCanciones = await client.GetAsync($"canciones/{usuarioId}/canciones"); // Endpoint que filtra las canciones por ArtistaId
            if (!respCanciones.IsSuccessStatusCode)
            {
                // Si no se pueden obtener las canciones del artista, devolver error
                ModelState.AddModelError("", "Error al obtener las canciones del artista.");
                return View("Error", CrearErrorModel());
            }

            // Leer la respuesta JSON con las canciones del artista
            var rawCanciones = await respCanciones.Content.ReadAsStringAsync();
            var cancionesArtista = JsonSerializer.Deserialize<List<CancionDto>>(rawCanciones, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewBag.CancionesArtista = cancionesArtista;
            ViewBag.PlaylistId = playlistId;

            // Verificar que la canción seleccionada exista en la lista de canciones del artista
            var cancionSeleccionada = cancionesArtista.FirstOrDefault(c => c.Id == cancionId);
            if (cancionSeleccionada == null)
            {
                // Si la canción no es del artista logueado, devolver mensaje de error
                ModelState.AddModelError("", "La canción seleccionada no pertenece al artista.");
                return View("Error", CrearErrorModel());
            }

            // Realizar la solicitud POST para agregar la canción a la playlist
            var resp = await client.PostAsync($"playlists/{playlistId}/agregar/{cancionId}", null);
            if (resp.IsSuccessStatusCode)
            {
                // Si la canción fue agregada correctamente, redirigir a la vista de detalles de la playlist
                return RedirectToAction(nameof(Detalle), new { id = playlistId });
            }

            // Si hubo algún error al agregar la canción, mostrar un error genérico
            ModelState.AddModelError("", "No se pudo agregar la canción a la playlist.");
            return View("Error", CrearErrorModel());
        }



        [HttpPost, Authorize]
        public async Task<IActionResult> QuitarMeGusta(int cancionId)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.DeleteAsync($"playlists/me-gusta/{cancionId}");
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(MeGusta));

            return View("Error", CrearErrorModel());
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> DarMeGusta(int cancionId)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.PostAsync($"playlists/me-gusta/{cancionId}", null);
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(MeGusta));

            return View("Error", CrearErrorModel());
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public async Task<IActionResult> MeGusta()
        {
            var client = ObtenerClienteConToken();
            var resp = await client.GetAsync("playlists/me-gusta");

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                // Nada en Me Gusta: devolvemos la vista con lista vacía y mensaje
                ViewBag.Message = "No tienes canciones marcadas como Me Gusta.";
                var emptyDto = new MeGustaRespuestaDto
                {
                    Nombre = null,
                    Total = 0,
                    Canciones = null
                };
                return View(emptyDto);
            }

            if (!resp.IsSuccessStatusCode)
            {
                // Otros errores
                return View("Error", CrearErrorModel());
            }

            var raw = await resp.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<MeGustaRespuestaDto>(raw, opts);

            return View(dto);
        }
    }
}