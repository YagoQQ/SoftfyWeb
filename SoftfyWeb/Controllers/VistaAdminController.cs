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
            var response = await cliente.GetAsync("https://localhost:7003/api/oyentes");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error al obtener los oyentes.";
                return View(new List<PerfilOyenteDto>());
            }

            var contenido = await response.Content.ReadAsStringAsync();
            var listaOyentes = JsonSerializer.Deserialize<List<PerfilOyenteDto>>(contenido, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(listaOyentes);
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
    }
}
