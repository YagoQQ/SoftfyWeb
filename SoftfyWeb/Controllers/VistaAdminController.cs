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
        public VistaAdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var jwt = User.FindFirst("jwt")?.Value; // Obtener el token desde los Claims
            if (!string.IsNullOrEmpty(jwt))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return client;
        }

        public async Task<IActionResult> IndexUsuario()
        {
            var cliente = ObtenerClienteConToken();


            var oyentes = await cliente.GetFromJsonAsync<List<PerfilOyenteDto>>("https://localhost:7003/api/oyentes");
            var artistas = await cliente.GetFromJsonAsync<List<PerfilArtistaDto>>("https://localhost:7003/api/Artistas");
            var usuariosBloqueados = await cliente.GetFromJsonAsync<List<Usuario>>("https://localhost:7003/api/Admin/usuarios-bloqueados");

            ViewBag.Oyentes = oyentes;
            ViewBag.Artistas = artistas;
            ViewBag.UsuariosBloqueados = usuariosBloqueados;
            
            return View();
        }

        public async Task<IActionResult> IndexCanciones()
        {
            var client = ObtenerClienteConToken();
            var canciones = await client.GetFromJsonAsync<List<Cancion>>("https://localhost:7003/api/canciones/canciones");
            return View(canciones);
            
        }

        [HttpPost]
        public async Task<IActionResult> EliminarCancion(int id)
        {
            var client = ObtenerClienteConToken();
            var response = await client.DeleteAsync($"https://localhost:7003/api/canciones/eliminar/{id}");

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo eliminar la canción.";

            return RedirectToAction("IndexCanciones");
        }

        [HttpGet]
        public async Task<IActionResult> IndexPlaylists()
        {
            var client = ObtenerClienteConToken();
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
            var client = ObtenerClienteConToken();
            var response = await client.DeleteAsync($"https://localhost:7003/api/playlists/{id}/eliminar");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo eliminar la playlist.";
            }

            return RedirectToAction("IndexPlaylists");
        }

        [AllowAnonymous]
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

            foreach (var cancion in canciones)
            {
                if (string.IsNullOrWhiteSpace(cancion.UrlArchivo))
                {
                    cancion.UrlArchivo = "#";
                }
            }

            ViewBag.PlaylistId = id;
            return View("VerCanciones", canciones);
        }

        [HttpPost]
        public async Task<IActionResult> BloquearUsuario(string email)
        {
            var client = ObtenerClienteConToken();
            var response = await client.PostAsync($"https://localhost:7003/api/Admin/bloquear/{email}", null);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo bloquear al usuario.";
            }
            else
            {
                TempData["Success"] = "Usuario bloqueado exitosamente.";
            }

            return RedirectToAction("IndexUsuario");
        }

        [HttpPost]
        public async Task<IActionResult> DesbloquearUsuario(string email)
        {
            var client = ObtenerClienteConToken(); // Llamar al método actualizado
            var response = await client.PostAsync($"https://localhost:7003/api/Admin/desbloquear/{email}", null);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo desbloquear al usuario.";
            }
            else
            {
                TempData["Success"] = "Usuario desbloqueado exitosamente.";
            }

            return RedirectToAction("IndexUsuario");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(string email)
        {
            var client = ObtenerClienteConToken(); // Llamar al método actualizado
            var response = await client.DeleteAsync($"https://localhost:7003/api/Admin/usuario/{email}");

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo eliminar el usuario.";

            return RedirectToAction("IndexUsuario");
        }
    }
}