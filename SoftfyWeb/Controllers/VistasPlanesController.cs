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

        public VistasPlanesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient ObtenerCliente()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> Index()
        {
            var client = ObtenerCliente();
            var response = await client.GetAsync("https://localhost:7003/api/Planes");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudieron cargar los planes.";
                return View(new List<Plan>());
            }

            var content = await response.Content.ReadAsStringAsync();
            var planes = JsonSerializer.Deserialize<List<Plan>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(planes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = ObtenerCliente();
            var response = await client.GetAsync($"https://localhost:7003/api/Planes/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var content = await response.Content.ReadAsStringAsync();
            var plan = JsonSerializer.Deserialize<Plan>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(plan);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Plan plan)
        {
            if (!ModelState.IsValid)
                return View(plan);

            var client = ObtenerCliente();
            var content = new StringContent(JsonSerializer.Serialize(plan), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://localhost:7003/api/Planes/registrar", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudo crear el plan.";
                return View(plan);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var client = ObtenerCliente();
            var response = await client.GetAsync($"https://localhost:7003/api/Planes/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var content = await response.Content.ReadAsStringAsync();
            var plan = JsonSerializer.Deserialize<Plan>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Plan plan)
        {
            if (!ModelState.IsValid)
                return View(plan);

            var client = ObtenerCliente();
            var content = new StringContent(JsonSerializer.Serialize(plan), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"https://localhost:7003/api/Planes/{id}", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudo actualizar el plan.";
                return View(plan);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var client = ObtenerCliente();
            var response = await client.GetAsync($"https://localhost:7003/api/Planes/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var content = await response.Content.ReadAsStringAsync();
            var plan = JsonSerializer.Deserialize<Plan>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(plan);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = ObtenerCliente();
            var response = await client.DeleteAsync($"https://localhost:7003/api/Planes/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo eliminar el plan.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
