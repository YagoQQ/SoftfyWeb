using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SoftfyWeb.Controllers
{
    [Authorize]
    public class VistasCancionesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public VistasCancionesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CrearCliente(bool incluirToken = false)
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            if (incluirToken)
            {
                var token = Request.Cookies["jwt_token"];
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task<T?> LeerComo<T>(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        private ErrorViewModel CrearErrorModel() =>
            new() { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var client = CrearCliente();
            var response = await client.GetAsync("canciones/canciones");
            if (!response.IsSuccessStatusCode) return View("Error", CrearErrorModel());

            var lista = await LeerComo<List<CancionRespuestaDto>>(response);
            return View(lista ?? new List<CancionRespuestaDto>());
        }

        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> MisCanciones()
        {
            var client = CrearCliente(true);
            var response = await client.GetAsync("canciones/mis-canciones");
            if (!response.IsSuccessStatusCode) return View("Error", CrearErrorModel());

            var lista = await LeerComo<List<CancionDto>>(response);
            return View(lista ?? new List<CancionDto>());
        }

        [Authorize(Roles = "Artista")]
        public IActionResult CrearCancion() => View(new CancionCrearDto());

        [HttpPost, Authorize(Roles = "Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCancion(CancionCrearDto dto, IFormFile archivoCancion)
        {
            if (!ModelState.IsValid) return View(dto);
            if (archivoCancion == null || archivoCancion.Length == 0)
            {
                ModelState.AddModelError("", "Debes seleccionar un archivo.");
                return View(dto);
            }

            try
            {
                using var form = new MultipartFormDataContent
                {
                    { new StringContent(dto.Titulo), nameof(dto.Titulo) },
                    { new StringContent(dto.Genero ?? ""), nameof(dto.Genero) },
                    { new StringContent(dto.FechaLanzamiento.ToString("o")), nameof(dto.FechaLanzamiento) }
                };

                await using var stream = archivoCancion.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(archivoCancion.ContentType);
                form.Add(fileContent, "archivoCancion", archivoCancion.FileName);

                var client = CrearCliente(true);
                var response = await client.PostAsync("canciones/crear", form);

                if (response.IsSuccessStatusCode) return RedirectToAction(nameof(MisCanciones));

                var errorContenido = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error al crear la canción: {errorContenido}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error inesperado: {ex.Message}");
            }

            return View(dto);
        }

        [HttpGet, Authorize(Roles = "Artista")]
        public async Task<IActionResult> EditarCancion(int id)
        {
            var client = CrearCliente(true);
            var response = await client.GetAsync($"canciones/{id}");
            if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(MisCanciones));

            var cancion = await LeerComo<CancionCrearDto>(response);
            if (cancion == null) return RedirectToAction(nameof(MisCanciones));

            ViewBag.CancionId = id;
            return View("Editar", cancion);
        }

        [HttpPost, Authorize(Roles = "Artista")]
        public async Task<IActionResult> ActualizarCancion(int id, [FromForm] CancionCrearDto song, IFormFile? nuevoArchivo)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Por favor, complete todos los campos.");
                return View("Editar", song);
            }

            using var formData = new MultipartFormDataContent
            {
                { new StringContent(song.Titulo), nameof(song.Titulo) },
                { new StringContent(song.Genero), nameof(song.Genero) },
                { new StringContent(song.FechaLanzamiento.ToString("o")), nameof(song.FechaLanzamiento) }
            };

            if (nuevoArchivo != null)
            {
                await using var stream = nuevoArchivo.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(nuevoArchivo.ContentType);
                formData.Add(fileContent, "nuevoArchivo", nuevoArchivo.FileName);
            }

            var client = CrearCliente(true);
            var response = await client.PutAsync($"canciones/editar/{id}", formData);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Hubo un problema al actualizar la canción.";
                return View("Editar", song);
            }

            return RedirectToAction(nameof(MisCanciones));
        }

        [AllowAnonymous]
        public async Task<IActionResult> VerCanciones(int id)
        {
            var client = CrearCliente(true);

            var cancionesResponse = await client.GetAsync($"playlists/{id}/canciones");
            var playlistResponse = await client.GetAsync($"playlists/{id}");

            if (!cancionesResponse.IsSuccessStatusCode || !playlistResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudieron obtener los datos.";
                return View(new List<Cancion>());
            }

            var canciones = await LeerComo<List<Cancion>>(cancionesResponse);
            var playlist = await LeerComo<Playlist>(playlistResponse);

            if (canciones == null || playlist == null)
            {
                ViewBag.Error = "Datos no encontrados.";
                return View(new List<Cancion>());
            }

            ViewBag.PlaylistNombre = playlist.Nombre;
            ViewBag.PlaylistId = id;

            foreach (var cancion in canciones)
            {
                if (string.IsNullOrWhiteSpace(cancion.UrlArchivo))
                    cancion.UrlArchivo = "#";
            }

            return View("VerCanciones", canciones);
        }
    }
}
