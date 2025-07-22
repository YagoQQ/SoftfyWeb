using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace SoftfyWeb.Controllers
{
    public class VistasSuscripcionesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasSuscripcionesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var jwt = User.FindFirst("jwt")?.Value;
            if (!string.IsNullOrEmpty(jwt))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            return client;
        }

        private ErrorViewModel CrearErrorModel()
        {
            string id = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return new ErrorViewModel { RequestId = id };
        }

        // Reutilizable: carga estado y miembros
        private async Task<SuscripcionEstadoDto> CargarEstadoYMiembrosAsync()
        {
            var client = ObtenerClienteConToken();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Trae estado
            var estado = await client.GetFromJsonAsync<SuscripcionEstadoDto>("suscripciones/estado");
            if (estado == null)
                return null;

            // Si es Premium, trae miembros
            if (estado.Tipo == "Premium")
            {
                var miembros = await client.GetFromJsonAsync<List<MiembroDto>>("suscripciones/miembros");
                estado.Miembros = miembros ?? new List<MiembroDto>();
            }
            return estado;
        }

        // GET: /VistasSuscripciones/Index
        public IActionResult Index() => RedirectToAction(nameof(Estado));

        // GET: /VistasSuscripciones/Estado
        public async Task<IActionResult> Estado()
        {
            var model = await CargarEstadoYMiembrosAsync();
            if (model == null)
                return View("Error", CrearErrorModel());

            // Extrae el email del usuario actual (fallback a Name si no hubiera claim)
            var currentEmail = User.FindFirstValue(ClaimTypes.Email)
                               ?? User.Identity.Name;
            ViewBag.CurrentEmail = currentEmail;

            return View(model);
        }

        // GET: /VistasSuscripciones/ActivarSuscripcion
        [HttpGet]
        public async Task<IActionResult> ActivarSuscripcion()
        {
            var client = ObtenerClienteConToken();
            var response = await client.GetAsync("planes");
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var planes = await response.Content.ReadFromJsonAsync<List<PlanDto>>();
            return View(planes ?? new List<PlanDto>());
        }

        // POST: /VistasSuscripciones/ActivarSuscripcion
        [HttpPost]
        public async Task<IActionResult> ActivarSuscripcion(int planId)
        {
            var client = ObtenerClienteConToken();

            // Llama a la API que crea la orden PayPal
            var response = await client.PostAsJsonAsync("pagos/crear-orden", planId);

            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var resultado = await response.Content.ReadFromJsonAsync<PayPalRespuestaDto>();

            return Redirect(resultado!.Url);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarMiembro(string email)
        {
            var client = ObtenerClienteConToken();
            var dto = new AgregarMiembroDto { Email = email };
            var response = await client.PostAsJsonAsync("suscripciones/agregar-miembro", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                var mensaje = "Error al agregar miembro";
                try
                {
                    using var doc = JsonDocument.Parse(errorJson);
                    mensaje = doc.RootElement.GetProperty("mensaje").GetString() ?? mensaje;
                }
                catch { }

                var model = await CargarEstadoYMiembrosAsync();
                ViewBag.ErrorMiembro = mensaje;
                return View("Estado", model);
            }
            return RedirectToAction(nameof(Estado));
        }

        // POST: /VistasSuscripciones/EliminarMiembro
        [HttpPost]
        public async Task<IActionResult> EliminarMiembro(string email)
        {
            var client = ObtenerClienteConToken();
            var dto = new EliminarMiembroDto { Email = email };
            var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(client.BaseAddress + "suscripciones/eliminar-miembro"))
            {
                Content = JsonContent.Create(dto)
            };
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                var mensaje = "Error al eliminar miembro";
                try
                {
                    using var doc = JsonDocument.Parse(errorJson);
                    mensaje = doc.RootElement.GetProperty("mensaje").GetString() ?? mensaje;
                }
                catch { }

                var model = await CargarEstadoYMiembrosAsync();
                ViewBag.ErrorMiembro = mensaje;
                return View("Estado", model);
            }
            return RedirectToAction(nameof(Estado));
        }

        // POST: /VistasSuscripciones/SalirDeSuscripcion
        [HttpPost]
        public async Task<IActionResult> SalirDeSuscripcion()
        {
            var client = ObtenerClienteConToken();
            var response = await client.PostAsync("suscripciones/salir-de-suscripcion", null);
            if (!response.IsSuccessStatusCode)
            {
                // ... manejar error ...
                return View("Estado", await CargarEstadoYMiembrosAsync());
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Info"] = "Suscripción cancelada. Por seguridad, inicia sesión de nuevo.";
            return RedirectToAction("Login", "VistasAuth");
        }

        // GET: /VistasSuscripciones/CancelarSuscripcion
        [HttpGet]
        public IActionResult CancelarSuscripcion() => View();

        // POST: /VistasSuscripciones/CancelarSuscripcion
        [HttpPost, ActionName("CancelarSuscripcion")]
        public async Task<IActionResult> ConfirmarCancelarSuscripcion()
        {
            var client = ObtenerClienteConToken();
            var response = await client.PostAsync("suscripciones/cancelar", null);
            if (!response.IsSuccessStatusCode)
            {
                // ... manejar error ...
                return View("Estado", await CargarEstadoYMiembrosAsync());
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Info"] = "Suscripción cancelada. Por seguridad, inicia sesión de nuevo.";
            return RedirectToAction("Login", "VistasAuth");
        }

        [HttpGet]
        public async Task<IActionResult> Exito(string token)
        {
            if (string.IsNullOrEmpty(token))
                return View("Error", CrearErrorModel());

            var client = ObtenerClienteConToken();

            // Llamar a la API que captura la orden
            var response = await client.PostAsync($"pagos/capturar-orden?token={token}", null);

            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            // Recarga estado actualizado
            var estado = await CargarEstadoYMiembrosAsync();
            if (estado == null)
                return View("Error", CrearErrorModel());

            return View("Estado", estado);
        }


    }
}