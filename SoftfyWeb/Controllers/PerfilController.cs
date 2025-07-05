using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SoftfyWeb.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PerfilController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Ver perfil de Artista o Usuario
        [HttpGet]
        public async Task<IActionResult> VerPerfil()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];

            // Verifica si el token JWT está presente
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("Token no encontrado.");
            }

            // Agregar el token en la cabecera Authorization
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Verificar perfil de Artista
            var response = await client.GetAsync("api/Artistas/mi-perfil");

            if (response.IsSuccessStatusCode)
            {
                var rawData = await response.Content.ReadAsStringAsync();
                var perfilArtista = JsonSerializer.Deserialize<Artista>(rawData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Si el perfil del artista es encontrado, lo pasamos a la vista
                ViewBag.TipoUsuario = "Artista";
                ViewBag.NombreArtistico = perfilArtista.NombreArtistico;
                ViewBag.FotoUrl = perfilArtista.FotoUrl;
                ViewBag.Biografia = perfilArtista.Biografia;

                return View("VerPerfil");
            }

            // Si no es Artista, intentamos obtenerlo como Usuario (Oyente)
            response = await client.GetAsync("api/Oyentes/mi-perfil");

            if (response.IsSuccessStatusCode)
            {
                var rawData = await response.Content.ReadAsStringAsync();
                var perfilOyente = JsonSerializer.Deserialize<Usuario>(rawData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Pasar los datos del perfil del Oyente a la vista
                ViewBag.TipoUsuario = "Oyente";
                ViewBag.Nombre = perfilOyente.Nombre;
                ViewBag.Apellido = perfilOyente.Apellido;

                return View("VerPerfil");
            }

            // Si no se encontró el perfil en la API, devolver un error
            return NotFound("Perfil no encontrado.");
        }



        // Editar perfil del usuario
        [HttpGet]
        public async Task<IActionResult> EditarPerfil()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("Token no encontrado.");
            }

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Obtener perfil del Artista
            var response = await client.GetAsync("api/Artistas/mi-perfil");

            if (response.IsSuccessStatusCode)
            {
                var rawData = await response.Content.ReadAsStringAsync();
                var perfilArtista = JsonSerializer.Deserialize<Artista>(rawData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View("EditarPerfilArtista", perfilArtista);
            }

            // Si no es Artista, obtenerlo como Usuario (Oyente)
            response = await client.GetAsync("api/Oyentes/mi-perfil");

            if (response.IsSuccessStatusCode)
            {
                var rawData = await response.Content.ReadAsStringAsync();
                var perfilOyente = JsonSerializer.Deserialize<PerfilOyenteDto>(rawData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View("EditarPerfilUsuario", perfilOyente);
            }

            return NotFound("No se encontró el perfil para editar.");
        }

        // Guardar cambios en el perfil del usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(Usuario model)
        {
            if (ModelState.IsValid)
            {
                var client = _httpClientFactory.CreateClient("SoftfyApi");
                var token = Request.Cookies["jwt_token"];
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Token no encontrado.");
                }

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var url = model.TipoUsuario == "Artista" ? "api/Artistas/editar" : "api/Oyentes/editar";
                var response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Perfil actualizado correctamente";
                    return RedirectToAction(nameof(VerPerfil));
                }
                else
                {
                    TempData["ErrorMessage"] = "Hubo un error al actualizar el perfil";
                }
            }

            return View(model);
        }
    }
}
