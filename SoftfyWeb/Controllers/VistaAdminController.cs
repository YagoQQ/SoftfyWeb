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
            var usuariosBloqueadosResp = await cliente.GetAsync("https://localhost:7003/api/Admin/usuarios-bloqueados");

            if (!oyentesResp.IsSuccessStatusCode || !artistasResp.IsSuccessStatusCode || !usuariosBloqueadosResp.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error al obtener usuarios.";
                return View(); // vista vacía si falla
            }

            var oyentesJson = await oyentesResp.Content.ReadAsStringAsync();
            var artistasJson = await artistasResp.Content.ReadAsStringAsync();
            var usuariosBloqueadosJson = await usuariosBloqueadosResp.Content.ReadAsStringAsync();

            var oyentes = JsonSerializer.Deserialize<List<PerfilOyenteDto>>(oyentesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var artistas = JsonSerializer.Deserialize<List<PerfilArtistaDto>>(artistasJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var usuariosBloqueados = JsonSerializer.Deserialize<List<Usuario>>(usuariosBloqueadosJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewBag.Oyentes = oyentes;
            ViewBag.Artistas = artistas;
            ViewBag.UsuariosBloqueados = usuariosBloqueados;

            return View();
        }


        public async Task<IActionResult> IndexCanciones()
        {
            var client = ObtenerClienteConToken();
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
            return View(canciones);
        }


        [HttpPost]
        public async Task<IActionResult> EliminarCancion(int id)
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"https://localhost:7003/api/canciones/eliminar/{id}");

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo eliminar la canción.";

            return RedirectToAction("IndexCanciones");
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
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
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
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
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
            var client = _httpClientFactory.CreateClient("Api");
            var token = Request.Cookies["jwt_token"];

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"https://localhost:7003/api/Admin/usuario/{email}");

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo eliminar el usuario.";

            return RedirectToAction("IndexUsuario");
        }
    }
}
