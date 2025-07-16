using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
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

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            HttpClient client = ObtenerCliente();
            HttpResponseMessage response = await client.GetAsync("canciones/canciones"); 
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lista = JsonConvert.DeserializeObject<List<CancionRespuestaDto>>(json);

            return View(lista);
        }


        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> MisCanciones()
        {
            var client = ObtenerClienteConToken();
            var response = await client.GetAsync("canciones/mis-canciones");
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lista = JsonConvert.DeserializeObject<List<CancionDto>>(json);

            return View(lista);
        }



        [Authorize(Roles = "Artista")]
        public IActionResult CrearCancion()
        {
            return View(new CancionCrearDto());
        }

        [HttpPost]
        [Authorize(Roles = "Artista")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCancion(CancionCrearDto dto, IFormFile archivoCancion)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (archivoCancion == null || archivoCancion.Length == 0)
            {
                ModelState.AddModelError("", "Debes seleccionar un archivo.");
                return View(dto);
            }

            try
            {
                var form = new MultipartFormDataContent();

                form.Add(new StringContent(dto.Titulo), nameof(dto.Titulo));
                form.Add(new StringContent(dto.Genero ?? ""), nameof(dto.Genero));
                form.Add(new StringContent(dto.FechaLanzamiento.ToString("o")), nameof(dto.FechaLanzamiento));

                var stream = new StreamContent(archivoCancion.OpenReadStream());
                stream.Headers.ContentType = new MediaTypeHeaderValue(archivoCancion.ContentType);
                form.Add(stream, "archivoCancion", archivoCancion.FileName);

                var client = ObtenerClienteConToken();
                var response = await client.PostAsync("canciones/crear", form);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction("MisCanciones");

                var errorContenido = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error al crear la canción: {errorContenido}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error inesperado: {ex.Message}");
            }

            return View(dto);
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(CrearErrorModel());
        }


        [HttpGet]
        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> EditarCancion(int id)
        {
            var client = ObtenerClienteConToken();
            var response = await client.GetAsync($"canciones/{id}");
            if (!response.IsSuccessStatusCode)
                return RedirectToAction("MisCanciones");

            var json = await response.Content.ReadAsStringAsync();
            var cancion = Newtonsoft.Json.JsonConvert.DeserializeObject<CancionCrearDto>(json);
            var dto = new CancionCrearDto
            {
                Titulo = cancion.Titulo,
                Genero = cancion.Genero,
                FechaLanzamiento = cancion.FechaLanzamiento
            };
            ViewBag.CancionId = id;
            return View("Editar",dto);
        }

        [HttpPost]
        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> ActualizarCancion(int id, [FromForm] CancionCrearDto song, IFormFile? nuevoArchivo)
        {
            if (song == null || string.IsNullOrEmpty(song.Titulo) || string.IsNullOrEmpty(song.Genero))
            {
                ModelState.AddModelError("", "Por favor, complete todos los campos.");
                return View("Editar", song);  // Regresar a la vista con los datos actuales
            }

            var client = ObtenerClienteConToken();
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(song.Titulo), "Titulo");
            formData.Add(new StringContent(song.Genero), "Genero");
            formData.Add(new StringContent(song.FechaLanzamiento.ToString("yyyy-MM-dd")), "FechaLanzamiento");

            if (nuevoArchivo != null)
            {
                var streamContent = new StreamContent(nuevoArchivo.OpenReadStream());
                streamContent.Headers.Add("Content-Type", nuevoArchivo.ContentType);
                formData.Add(streamContent, "nuevoArchivo", nuevoArchivo.FileName);
            }

            var response = await client.PutAsync($"canciones/editar/{id}", formData); // Aquí usamos PUT ya que es la acción de actualización en la API

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Hubo un problema al actualizar la canción.";
                return View("Editar", song);  // Regresar a la vista si hay error
            }

            ViewBag.Mensaje = "Canción actualizada correctamente.";
            return RedirectToAction("MisCanciones");  // Redirigir a la página de Mis Canciones
        }


    }
}
