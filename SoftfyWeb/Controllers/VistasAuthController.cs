using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SoftfyWeb.Controllers
{
    public class VistasAuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasAuthController(IHttpClientFactory httpClientFactory)
            => _httpClientFactory = httpClientFactory;

        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            ViewData["ShowSearchBar"] = false;
            if (!IsValidEmail(dto.Email))
                ModelState.AddModelError(nameof(dto.Email), "Correo inválido.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsync(
                "https://localhost:7003/api/auth/forgot-password",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", raw);
                return View(dto);
            }
            TempData["Info"] = "Si el correo existe, recibirás instrucciones para restablecer tu contraseña.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token, string email)
            => View(new ResetPasswordDto { Email = email, Token = token });

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            ViewData["ShowSearchBar"] = false;
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                ModelState.AddModelError(nameof(dto.NewPassword), "La contraseña debe tener al menos 6 caracteres.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsync(
                "https://localhost:7003/api/auth/reset-password",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", raw);
                return View(dto);
            }
            TempData["Info"] = "Contraseña restablecida correctamente. Ahora puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            ViewData["ShowSearchBar"] = false;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("jwt_token");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult RegistroArtista() => View();

        [HttpGet]
        public IActionResult Registro() => View(new UsuarioRegistroDto());

        [HttpPost]
        public async Task<IActionResult> Registro(UsuarioRegistroDto dto)
        {
            ViewData["ShowSearchBar"] = false;
            if (!EsContrasenaSegura(dto.Password))
                ModelState.AddModelError(nameof(dto.Password), "Debe tener ≥6 caracteres, 1 mayúscula y 1 número.");
            if (!IsValidEmail(dto.Email))
                ModelState.AddModelError(nameof(dto.Email), "Correo inválido.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient();
            var url = dto.TipoUsuario == "Artista"
                ? "https://localhost:7003/api/auth/registro-artista"
                : "https://localhost:7003/api/auth/registro";
            var resp = await client.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", raw);
                return View(dto);
            }
            TempData["RegistroOk"] = "¡Registro exitoso! Revisa tu correo y luego inicia sesión.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ShowSearchBar"] = false;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Info = TempData["RegistroOk"];
            return View(new UsuarioLoginDto());
        }

        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto dto, string returnUrl = null)
        {
            ViewData["ShowSearchBar"] = false;
            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsync(
                "https://localhost:7003/api/auth/login",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
            );
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("error", out var error) &&
                        error.GetString().Contains("confirmar tu correo"))
                    {
                        ViewBag.Error = "Debes confirmar tu correo antes de iniciar sesión.";
                    }
                    else if (root.TryGetProperty("error", out error) &&
                             error.GetString().Contains("bloqueada"))
                    {
                        ViewBag.Error = "Tu cuenta está bloqueada. Intenta nuevamente después de 1 minuto.";
                    }
                    else
                    {
                        ViewBag.Error = "Credenciales inválidas.";
                    }
                }
                catch (JsonException)
                {
                    ViewBag.Error = raw;
                }
                return View(dto);
            }

            var token = JsonDocument.Parse(raw)
                                   .RootElement
                                   .GetProperty("token")
                                   .GetString();

            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var identity = new ClaimsIdentity(jwtToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                    IsPersistent = false
                }
            );

            if (role == "Artista")
                return RedirectToAction(nameof(BienvenidoArtista));
            if (role == "Oyente")
                return RedirectToAction(nameof(BienvenidoOyente));
            if (role == "Oyente Premium")
                return RedirectToAction(nameof(BienvenidoOyentePremium));

            return RedirectToAction(nameof(Bienvenido));
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            ViewData["ShowSearchBar"] = false;
            var client = _httpClientFactory.CreateClient();
            var resp = await client.GetAsync(
                $"https://localhost:7003/api/auth/confirmar-email?userId={userId}&token={token}"
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Hubo un error al confirmar tu correo.";
                return RedirectToAction(nameof(Login));
            }
            TempData["Info"] = "Tu correo ha sido confirmado correctamente. Ahora puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> BienvenidoArtista()
        {
            var nombreArtistico = User.Identity.Name;
            try
            {
                var client = ObtenerClienteConToken();
                var resp = await client.GetAsync("artistas/mi-perfil");
                if (resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync();
                    var perfil = JsonSerializer.Deserialize<PerfilArtistaDto>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (perfil != null)
                        nombreArtistico = perfil.NombreArtistico;
                }
            }
            catch
            {
                // opcional: loguear el error
            }
            ViewBag.ArtistaNombre = nombreArtistico;
            return View();
        }

        public async Task<IActionResult> BienvenidoOyente()
        {
            var nombreOyente = User.Identity.Name;

            // Obtener nombre del Oyente desde el API
            try
            {
                var client = ObtenerClienteConToken(); // Método para obtener el cliente HTTP con el token de autenticación
                var resp = await client.GetAsync("oyentes/mi-perfil"); // Llamada al endpoint para obtener el perfil del oyente
                if (resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync();
                    var perfil = JsonSerializer.Deserialize<PerfilOyenteDto>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (perfil != null)
                    {
                        nombreOyente = $"{perfil.Nombre} {perfil.Apellido}";
                    }
                }
            }
            catch
            {
                // Si hay algún error al obtener el perfil, no hacer nada y mantener el nombre de la sesión
            }

            ViewBag.OyenteNombre = nombreOyente;

            // Obtener todas las canciones del sistema desde la API usando el endpoint proporcionado
            var clientCanciones = ObtenerClienteConToken();
            var respCanciones = await clientCanciones.GetAsync("https://localhost:7003/api/Canciones/canciones"); // Endpoint correcto
            var todasCanciones = new List<CancionRespuestaDto>();
            if (respCanciones.IsSuccessStatusCode)
            {
                var rawCanciones = await respCanciones.Content.ReadAsStringAsync();
                todasCanciones = JsonSerializer.Deserialize<List<CancionRespuestaDto>>(rawCanciones,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Asegurarse de que la URL del archivo esté correctamente formada
                foreach (var cancion in todasCanciones)
                {
                    var nombreArchivo = Path.GetFileName(cancion.UrlArchivo);
                    cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
                }
            }

            // Pasar las canciones al ViewBag
            ViewBag.TodasCanciones = todasCanciones;

            return View();
        }

        public async Task<IActionResult> BienvenidoOyentePremium()
        {
            var nombreOyente = User.Identity.Name;

            // Obtener nombre del Oyente desde el API
            try
            {
                var client = ObtenerClienteConToken(); // Método para obtener el cliente HTTP con el token de autenticación
                var resp = await client.GetAsync("oyentes/mi-perfil"); // Llamada al endpoint para obtener el perfil del oyente
                if (resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync();
                    var perfil = JsonSerializer.Deserialize<PerfilOyenteDto>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (perfil != null)
                    {
                        nombreOyente = $"{perfil.Nombre} {perfil.Apellido}";
                    }
                }
            }
            catch
            {
                // Si hay algún error al obtener el perfil, no hacer nada y mantener el nombre de la sesión
            }

            ViewBag.OyenteNombre = nombreOyente;

            // Obtener todas las canciones del sistema desde la API usando el endpoint proporcionado
            var clientCanciones = ObtenerClienteConToken();
            var respCanciones = await clientCanciones.GetAsync("https://localhost:7003/api/Canciones/canciones"); // Endpoint correcto
            var todasCanciones = new List<CancionDto>();
            if (respCanciones.IsSuccessStatusCode)
            {
                var rawCanciones = await respCanciones.Content.ReadAsStringAsync();
                todasCanciones = JsonSerializer.Deserialize<List<CancionDto>>(rawCanciones,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Asegurarse de que la URL del archivo esté correctamente formada
                foreach (var cancion in todasCanciones)
                {
                    var nombreArchivo = Path.GetFileName(cancion.UrlArchivo);
                    cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
                }
            }

            // Pasar las canciones al ViewBag
            ViewBag.TodasCanciones = todasCanciones;

            return View();
        }


        public IActionResult Bienvenido() => View();

        // Métodos auxiliares
        private bool EsContrasenaSegura(string pwd) =>
            !string.IsNullOrEmpty(pwd)
            && pwd.Length >= 6
            && pwd.Any(char.IsUpper)
            && pwd.Any(char.IsDigit);

        private bool IsValidEmail(string em)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(em);
                return addr.Address == em;
            }
            catch { return false; }
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string termino)
        {
            if (string.IsNullOrEmpty(termino))
            {
                ViewBag.Error = "Por favor, ingrese un término de búsqueda.";
                return View();
            }

            var client = _httpClientFactory.CreateClient();

            // Intentamos buscar canciones
            var cancionesResponse = await client.GetAsync($"https://localhost:7003/api/Canciones/canciones/nombre?nombre={termino}");
            var artistasResponse = await client.GetAsync($"https://localhost:7003/api/Artistas/artista/{termino}/perfil");

            // Verificar el estado de la respuesta para canciones
            if (cancionesResponse.IsSuccessStatusCode)
            {
                var cancionesJson = await cancionesResponse.Content.ReadAsStringAsync();
                var canciones = JsonSerializer.Deserialize<List<CancionRespuestaDto>>(cancionesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (canciones != null && canciones.Any())
                {
                    ViewBag.Canciones = canciones;  // Guardamos las canciones para la vista
                }
                else
                {
                    ViewBag.Error = "No se encontraron canciones.";
                }
            }

            // Verificar el estado de la respuesta para artistas
            if (artistasResponse.IsSuccessStatusCode)
            {
                var artistasJson = await artistasResponse.Content.ReadAsStringAsync();
                var artista = JsonSerializer.Deserialize<Artista>(artistasJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (artista != null)
                {
                    if (!string.IsNullOrEmpty(artista.FotoUrl))
                    {
                        artista.FotoUrl = $"https://localhost:7003/api/artistas/foto/{artista.FotoUrl}";
                    }

                    ViewBag.Artista = artista;
                }
                else
                {
                    ViewBag.Error = "No se encontró el artista.";
                }
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerPerfil()
        {
            var client = ObtenerClienteConToken();
            var rol = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (rol == "Artista")
            {
                var response = await client.GetAsync("https://localhost:7003/api/Artistas/mi-perfil");
                if (response.IsSuccessStatusCode)
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    var perfil = JsonSerializer.Deserialize<PerfilArtistaDto>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (perfil != null)
                    {
                        ViewBag.TipoUsuario = "Artista";
                        ViewBag.NombreArtistico = perfil.NombreArtistico;
                        ViewBag.FotoUrl = perfil.FotoUrl;
                        ViewBag.Biografia = perfil.Biografia;
                        return View("VerPerfilArtista");
                    }
                }
            }

            if (rol == "Oyente" || rol == "Oyente Premium")
            {
                var response = await client.GetAsync("https://localhost:7003/api/Oyentes/mi-perfil");
                if (response.IsSuccessStatusCode)
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    var perfil = JsonSerializer.Deserialize<PerfilOyenteDto>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (perfil != null)
                    {
                        ViewBag.TipoUsuario = "Oyente";
                        ViewBag.Nombre = perfil.Nombre;
                        ViewBag.Apellido = perfil.Apellido;
                        return View("VerPerfilOyente");
                    }
                }
            }

            return NotFound("Perfil no encontrado.");
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarPerfilArtista(string NombreArtistico, string Biografia, IFormFile Foto)
        {
            var client = ObtenerClienteConToken();

            var form = new MultipartFormDataContent();
            form.Add(new StringContent(NombreArtistico ?? ""), "nombreArtistico");
            form.Add(new StringContent(Biografia ?? ""), "biografia");

            if (Foto != null && Foto.Length > 0)
            {
                var streamContent = new StreamContent(Foto.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(Foto.ContentType);
                form.Add(streamContent, "foto", Foto.FileName);
            }

            var response = await client.PutAsync("https://localhost:7003/api/artistas/actualizar", form);

            if (response.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Perfil actualizado correctamente.";
                return RedirectToAction("VerPerfil");
            }

            TempData["Error"] = "Error al actualizar perfil.";
            return RedirectToAction("VerPerfil");
        }


        [HttpPost]
        public async Task<IActionResult> ActualizarPerfilOyente(string Nombre, string Apellido)
        {
            var client = ObtenerClienteConToken();

            var jsonBody = new
            {
                nombre = Nombre,
                apellido = Apellido
            };

            var content = new StringContent(JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");

            var response = await client.PutAsync("https://localhost:7003/api/oyentes/actualizar", content);
            var respuestaTexto = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Perfil actualizado correctamente.";
                return RedirectToAction("VerPerfil");
            }

            TempData["Error"] = $"Error al actualizar perfil: {respuestaTexto}";
            return RedirectToAction("VerPerfil");
        }

    }

}
