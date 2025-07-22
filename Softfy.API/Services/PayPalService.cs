using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;      // Ajusta el namespace según tu proyecto
using SoftfyWeb.Modelos;  // Para acceder a la entidad Plan o equivalente
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

public class PayPalService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context; // Tu DbContext

    public PayPalService(HttpClient httpClient, IConfiguration config, ApplicationDbContext context)
    {
        _httpClient = httpClient;
        _config = config;
        _context = context;
    }

    public async Task<string> CrearOrdenAsync(int planId)
    {
        // 1. Obtener el plan de la base de datos
        var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null)
            throw new Exception("Plan no encontrado");

        // 2. Obtener token OAuth2 de PayPal
        var clientId = _config["PayPal:ClientId"];
        var secret = _config["PayPal:ClientSecret"];
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v1/oauth2/token");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        tokenRequest.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var tokenResponse = await _httpClient.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        var accessToken = tokenData.GetProperty("access_token").GetString();

        // 3. Crear la orden en PayPal usando el precio del plan
        var orderRequest = new HttpRequestMessage(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v2/checkout/orders");
        orderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var orderBody = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = "USD",
                        value = plan.Precio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                return_url = "https://localhost:7130/VistasSuscripciones/Estado",
                cancel_url = "https://localhost:7130/VistasSuscripciones/ActivarSuscripcion"
            }
        };

        orderRequest.Content = new StringContent(JsonSerializer.Serialize(orderBody), Encoding.UTF8, "application/json");

        var orderResponse = await _httpClient.SendAsync(orderRequest);
        orderResponse.EnsureSuccessStatusCode();
        var orderResult = await orderResponse.Content.ReadAsStringAsync();

        return orderResult;
    }
}
