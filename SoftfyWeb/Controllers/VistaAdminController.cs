using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SoftfyWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VistaAdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string apiUrl = "https://localhost:7003/api/Admin";

        public VistaAdminController(IHttpClientFactory httpClientFactory)
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

        public async Task<IActionResult> IndexUsuario()
        {
            var cliente = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];

            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Token no encontrado.";
                return RedirectToAction("Login", "VistasAuth");
            }

            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var oyentesResp = await cliente.GetAsync("https://localhost:7003/api/oyentes");
            var artistasResp = await cliente.GetAsync("https://localhost:7003/api/Artistas");

            if (!oyentesResp.IsSuccessStatusCode || !artistasResp.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error al obtener usuarios.";
                return View(); // vista vacía si falla
            }

            var oyentesJson = await oyentesResp.Content.ReadAsStringAsync();
            var artistasJson = await artistasResp.Content.ReadAsStringAsync();

            var oyentes = JsonSerializer.Deserialize<List<PerfilOyenteDto>>(oyentesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var artistas = JsonSerializer.Deserialize<List<PerfilArtistaDto>>(artistasJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewBag.Oyentes = oyentes;
            ViewBag.Artistas = artistas;

            return View();
        }


        public async Task<IActionResult> IndexCanciones()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");

            var response = await client.GetAsync("https://localhost:7003/api/canciones/canciones");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error al cargar canciones.";
                return View(new List<Cancion>());
            }

            var content = await response.Content.ReadAsStringAsync();
            var canciones = JsonSerializer.Deserialize<List<Cancion>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Aquí transformamos la URL del archivo para que apunte al endpoint de reproducción
            foreach (var cancion in canciones)
            {
                var nombreArchivo = Path.GetFileName(cancion.UrlArchivo); // extrae solo el nombre.mp3
                cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
            }

            return View(canciones);
        }


        [HttpPost]
        public async Task<IActionResult> EliminarCancion(int id)
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"]; // asegúrate de que coincida

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"https://localhost:7003/api/Canciones/eliminar/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo eliminar la canción.";
            }

            return RedirectToAction("IndexCanciones");
        }


        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(string email)
        {
            var client = _httpClientFactory.CreateClient("Api");
            var token = Request.Cookies["jwt_token"]; // o "jwt" si usas ese nombre

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"https://localhost:7003/api/Admin/usuario/{email}");

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo eliminar el usuario.";

            return RedirectToAction("IndexUsuario");
        }

        [HttpGet]
        public async Task<IActionResult> IndexPlaylists()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("https://localhost:7003/api/Playlists/todas");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error al obtener las playlists.";
                return View(new List<PlaylistDto>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var playlists = JsonSerializer.Deserialize<List<PlaylistDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View("IndexPlaylists", playlists);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarPlaylist(int id)
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"https://localhost:7003/api/playlists/{id}/eliminar");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo eliminar la playlist.";
            }

            return RedirectToAction("IndexPlaylists");
        }


        [HttpGet]
        public async Task<IActionResult> VerCanciones(int id)
        {
            var client = ObtenerClienteConToken();
            var response = await client.GetAsync($"https://localhost:7003/api/playlists/{id}/canciones");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudieron obtener las canciones de la playlist.";
                return View(new List<PlaylistCancionDto>());
            }

            var contenido = await response.Content.ReadAsStringAsync();
            var canciones = JsonSerializer.Deserialize<List<PlaylistCancionDto>>(contenido,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // ✅ Ajustar URL del archivo para reproducción correcta
            foreach (var cancion in canciones)
            {
                if (!string.IsNullOrWhiteSpace(cancion.UrlArchivo))
                {
                    var nombreArchivo = Path.GetFileName(cancion.UrlArchivo);
                    cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
                }
            }

            ViewBag.PlaylistId = id;
            return View("VerCanciones", canciones);
        }

    }
}
