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
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private const string ApiBase = "https://localhost:7003/api";

        public VistaAdminController(IHttpClientFactory httpClientFactory)
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

        // ✅ Usuarios
        public async Task<IActionResult> IndexUsuario()
        {
            var oyentes = await GetApiData<List<PerfilOyenteDto>>($"{ApiBase}/oyentes");
            var artistas = await GetApiData<List<PerfilArtistaDto>>($"{ApiBase}/Artistas");
            var bloqueados = await GetApiData<List<Usuario>>($"{ApiBase}/Admin/usuarios-bloqueados");

            if (oyentes == null || artistas == null || bloqueados == null)
            {
                ViewBag.Error = "Error al obtener usuarios.";
                return View();
            }

            ViewBag.Oyentes = oyentes;
            ViewBag.Artistas = artistas;
            ViewBag.UsuariosBloqueados = bloqueados;

            return View();
        }

        // ✅ Canciones
        public async Task<IActionResult> IndexCanciones()
        {
            var canciones = await GetApiData<List<Cancion>>($"{ApiBase}/canciones/canciones");
            return canciones == null ? View("Error") : View(canciones);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarCancion(int id)
        {
            await EjecutarAccionDelete($"{ApiBase}/canciones/eliminar/{id}", "No se pudo eliminar la canción.");
            return RedirectToAction(nameof(IndexCanciones));
        }

        // ✅ Playlists
        public async Task<IActionResult> IndexPlaylists()
        {
            var playlists = await GetApiData<List<PlaylistDto>>($"{ApiBase}/Playlists/todas");
            return playlists == null ? View("Error") : View("IndexPlaylists", playlists);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarPlaylist(int id)
        {
            await EjecutarAccionDelete($"{ApiBase}/playlists/{id}/eliminar", "No se pudo eliminar la playlist.");
            return RedirectToAction(nameof(IndexPlaylists));
        }

        // ✅ Canciones en una Playlist
        [AllowAnonymous]
        public async Task<IActionResult> VerCanciones(int id)
        {
            var canciones = await GetApiData<List<PlaylistCancionDto>>($"{ApiBase}/playlists/{id}/canciones");
            if (canciones == null)
            {
                ViewBag.Error = "No se pudieron obtener las canciones.";
                return View(new List<PlaylistCancionDto>());
            }

            foreach (var c in canciones)
                c.UrlArchivo ??= "#";

            ViewBag.PlaylistId = id;
            return View("VerCanciones", canciones);
        }

        // ✅ Bloquear, Desbloquear, Eliminar Usuario (unificados)
        [HttpPost]
        public async Task<IActionResult> BloquearUsuario(string email) =>
            await EjecutarAccionPost($"{ApiBase}/Admin/bloquear/{email}", "Usuario bloqueado exitosamente.", "No se pudo bloquear al usuario.", nameof(IndexUsuario));

        [HttpPost]
        public async Task<IActionResult> DesbloquearUsuario(string email) =>
            await EjecutarAccionPost($"{ApiBase}/Admin/desbloquear/{email}", "Usuario desbloqueado exitosamente.", "No se pudo desbloquear al usuario.", nameof(IndexUsuario));

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(string email)
        {
            await EjecutarAccionDelete($"{ApiBase}/Admin/usuario/{email}", "No se pudo eliminar el usuario.");
            return RedirectToAction(nameof(IndexUsuario));
        }

        // ✅ Métodos auxiliares para reducir código repetido
        private async Task EjecutarAccionDelete(string url, string errorMsg)
        {
            var client = CrearCliente();
            var resp = await client.DeleteAsync(url);
            if (!resp.IsSuccessStatusCode) TempData["Error"] = errorMsg;
        }

        private async Task<IActionResult> EjecutarAccionPost(string url, string successMsg, string errorMsg, string redirectAction)
        {
            var client = CrearCliente();
            var resp = await client.PostAsync(url, null);
            TempData[resp.IsSuccessStatusCode ? "Success" : "Error"] = resp.IsSuccessStatusCode ? successMsg : errorMsg;
            return RedirectToAction(redirectAction);
        }
    }
}
