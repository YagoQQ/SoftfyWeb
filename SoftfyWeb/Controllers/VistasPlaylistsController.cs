using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SoftfyWeb.Controllers
{
    [Authorize]
    public class VistasPlaylistsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string ApiBase = "playlists";

        public VistasPlaylistsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CrearCliente()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<T?> GetApiData<T>(string url)
        {
            var client = CrearCliente();
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return default;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        // ✅ Listar playlists del usuario
        public async Task<IActionResult> Index()
        {
            var listas = await GetApiData<List<PlaylistDto>>($"{ApiBase}/mis-playlists");
            return listas == null ? View("Error") : View(listas);
        }

        [Authorize(Roles = "OyentePremium,Artista,Admin")]
        public IActionResult Crear() => View();

        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError("", "El nombre es requerido");
                return View();
            }

            var client = CrearCliente();
            var resp = await client.PostAsJsonAsync($"{ApiBase}/crear", nombre);

            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", await resp.Content.ReadAsStringAsync());
            return View();
        }

        [Authorize(Roles = "OyentePremium,Artista,Admin")]
        public async Task<IActionResult> Detalle(int id)
        {
            var canciones = await GetApiData<List<PlaylistCancionDto>>($"{ApiBase}/{id}/canciones");
            if (canciones == null) return View("Error");

            canciones = canciones.Where(c => !string.IsNullOrWhiteSpace(c.UrlArchivo)).ToList();

            var cancionesArtista = await GetApiData<List<CancionDto>>("canciones/mis-canciones") ?? new();

            ViewBag.PlaylistId = id;
            ViewBag.CancionesArtista = cancionesArtista;

            return View(canciones);
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista,Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Renombrar(int id, string nuevoNombre)
        {
            var client = CrearCliente();
            var resp = await client.PutAsJsonAsync($"{ApiBase}/{id}/renombrar", nuevoNombre);
            TempData["Error"] = resp.IsSuccessStatusCode ? null : await resp.Content.ReadAsStringAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista,Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var client = CrearCliente();
            var resp = await client.DeleteAsync($"{ApiBase}/{id}/eliminar");
            return resp.IsSuccessStatusCode ? RedirectToAction(nameof(Index)) : View("Error");
        }

        [HttpPost]
        public async Task<IActionResult> AgregarCancion(int playlistId, int cancionId)
        {
            var client = CrearCliente();
            var resp = await client.PostAsync($"{ApiBase}/{playlistId}/agregar/{cancionId}", null);
            if (!resp.IsSuccessStatusCode) TempData["Error"] = "No se pudo agregar la canción.";
            return RedirectToAction(nameof(Detalle), new { id = playlistId });
        }

        [HttpPost]
        public async Task<IActionResult> QuitarCancion(int playlistId, int cancionId)
        {
            var client = CrearCliente();
            await client.DeleteAsync($"{ApiBase}/{playlistId}/quitar/{cancionId}");
            return RedirectToAction(nameof(Detalle), new { id = playlistId });
        }

        [HttpPost]
        public async Task<IActionResult> DarMeGusta(int cancionId)
        {
            var client = CrearCliente();
            await client.PostAsync($"{ApiBase}/me-gusta/{cancionId}", null);
            return RedirectToAction(nameof(MeGusta));
        }

        [HttpPost]
        public async Task<IActionResult> QuitarMeGusta(int cancionId)
        {
            var client = CrearCliente();
            await client.DeleteAsync($"{ApiBase}/me-gusta/{cancionId}");
            return RedirectToAction(nameof(MeGusta));
        }

        public async Task<IActionResult> MeGusta()
        {
            var dto = await GetApiData<MeGustaRespuestaDto>($"{ApiBase}/me-gusta");
            if (dto == null || dto.Canciones == null)
                ViewBag.Message = "No tienes canciones marcadas como Me Gusta.";
            return View(dto ?? new MeGustaRespuestaDto { Canciones = new List<CancionDto>(), Total = 0 });
        }

        [AllowAnonymous]
        public async Task<IActionResult> PlaylistPorArtista(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Mensaje = "No se pudo obtener el correo del artista.";
                return View(new List<PlaylistDto>());
            }

            var playlists = await GetApiData<List<PlaylistDto>>($"{ApiBase}/correo/{email}/playlists");
            if (playlists == null)
            {
                ViewBag.Mensaje = "No se pudieron obtener las playlists del artista.";
                return View(new List<PlaylistDto>());
            }

            ViewBag.Email = email;
            return View(playlists);
        }
    }
}
