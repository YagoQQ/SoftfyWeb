using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Softfy.API.Services;

namespace Softfy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagosController : ControllerBase
    {
        private readonly PayPalService _payPalService;

        public PagosController(PayPalService payPalService)
        {
            _payPalService = payPalService;
        }

        [HttpPost("crear-orden")]
        public async Task<IActionResult> CrearOrden([FromBody] int planId)
        {
            var orderResultJson = await _payPalService.CrearOrdenAsync(planId);

            // Extrae el link de aprobación (el que PayPal te da para redirigir al usuario)
            var doc = JsonDocument.Parse(orderResultJson);
            var approveLink = doc.RootElement
                .GetProperty("links")
                .EnumerateArray()
                .FirstOrDefault(x => x.GetProperty("rel").GetString() == "approve")
                .GetProperty("href")
                .GetString();

            return Ok(new { url = approveLink });
        }
    }
}
