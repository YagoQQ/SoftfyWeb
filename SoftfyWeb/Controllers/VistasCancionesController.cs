using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SoftfyWeb.Controllers
{
    [Authorize]
    public class VistasCancionesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasCancionesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization
                      = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private HttpClient ObtenerCliente()
        {
            return _httpClientFactory.CreateClient("SoftfyApi");
        }

        private ErrorViewModel CrearErrorModel()
        {
            string id = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return new ErrorViewModel { RequestId = id };
        }

        // 1) Listar todas las canciones (para oyentes)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            HttpClient client = ObtenerCliente();
            HttpResponseMessage response = await client.GetAsync("canciones/canciones"); // Solicita todas las canciones
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lista = JsonSerializer.Deserialize<List<CancionRespuestaDto>>(json, opciones);

            // Asegurarse de que la URL del archivo esté correctamente formada
            foreach (var cancion in lista)
            {
                var nombreArchivo = Path.GetFileName(cancion.UrlArchivo);
                cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
            }

            return View(lista);
        }

        // 2) Listar mis canciones (solo Artista)
        // --- MisCanciones (ARTISTA sólo) ---
        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> MisCanciones()
        {
            var client = ObtenerClienteConToken();
            var response = await client.GetAsync("canciones/mis-canciones");
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lista = JsonSerializer.Deserialize<List<CancionRespuestaDto>>(json, opts);

            // Asignar la URL completa para cada archivo, usando el endpoint correcto
            foreach (var cancion in lista)
            {
                // Si UrlArchivo contiene una ruta completa, extraer solo el nombre del archivo
                var nombreArchivo = Path.GetFileName(cancion.UrlArchivo);

                // Asignar la URL correcta con el nombre del archivo
                cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
            }

            return View(lista);
        }



        // 3) Formulario de creación
        [Authorize(Roles = "Artista")]
        public IActionResult CrearCancion()
        {
            return View(new CancionCrearDto());
        }

        // --- CrearCancion POST ---
        [HttpPost, Authorize(Roles = "Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCancion(CancionCrearDto dto, IFormFile archivoCancion)
        {
            if (!ModelState.IsValid) return View(dto);

            if (archivoCancion == null || archivoCancion.Length == 0)
            {
                ModelState.AddModelError("", "Debes seleccionar un archivo.");
                return View(dto);
            }

            var form = new MultipartFormDataContent();
            form.Add(new StringContent(dto.Titulo), nameof(dto.Titulo));
            form.Add(new StringContent(dto.Genero ?? ""), nameof(dto.Genero));
            form.Add(new StringContent(dto.FechaLanzamiento.ToString("o")),
                     nameof(dto.FechaLanzamiento));

            var stream = new StreamContent(archivoCancion.OpenReadStream());
            stream.Headers.ContentType =
                new MediaTypeHeaderValue(archivoCancion.ContentType);
            form.Add(stream, "archivoCancion", archivoCancion.FileName);

            var client = ObtenerClienteConToken();
            var response = await client.PostAsync("canciones/crear", form);
            if (response.IsSuccessStatusCode)
                return RedirectToAction("MisCanciones");

            var err = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", err);
            return View(dto);
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(CrearErrorModel());
        }
    }
}
