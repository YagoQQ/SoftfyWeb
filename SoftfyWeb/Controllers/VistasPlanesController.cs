using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Modelos;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SoftfyWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VistasPlanesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string ApiBase = "planes"; // Usamos el endpoint base relativo

        public VistasPlanesController(IHttpClientFactory httpClientFactory)
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

        private async Task<T?> LeerComo<T>(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<IActionResult> Index()
        {
            var client = CrearCliente();
            var response = await client.GetAsync(ApiBase);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudieron cargar los planes.";
                return View(new List<Plan>());
            }

            var planes = await LeerComo<List<Plan>>(response);
            return View(planes ?? new List<Plan>());
        }

        public async Task<IActionResult> Details(int id)
        {
            var plan = await ObtenerPlanPorId(id);
            return plan == null ? NotFound() : View(plan);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Plan plan)
        {
            if (!ModelState.IsValid) return View(plan);

            var client = CrearCliente();
            var content = new StringContent(JsonSerializer.Serialize(plan), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ApiBase}/registrar", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudo crear el plan.";
                return View(plan);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var plan = await ObtenerPlanPorId(id);
            return plan == null ? NotFound() : View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Plan plan)
        {
            if (!ModelState.IsValid) return View(plan);

            var client = CrearCliente();
            var content = new StringContent(JsonSerializer.Serialize(plan), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{ApiBase}/{id}", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudo actualizar el plan.";
                return View(plan);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var plan = await ObtenerPlanPorId(id);
            return plan == null ? NotFound() : View(plan);
        }

        [HttpPost, ActionName(nameof(Delete))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = CrearCliente();
            var response = await client.DeleteAsync($"{ApiBase}/{id}");

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo eliminar el plan.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<Plan?> ObtenerPlanPorId(int id)
        {
            var client = CrearCliente();
            var response = await client.GetAsync($"{ApiBase}/{id}");
            return response.IsSuccessStatusCode ? await LeerComo<Plan>(response) : null;
        }
    }
}
