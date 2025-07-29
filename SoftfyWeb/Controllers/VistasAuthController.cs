using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Models;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

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


        [HttpGet]
        public IActionResult ForgotPassword() => View();


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!IsValidEmail(dto.Email))
                ModelState.AddModelError(nameof(dto.Email), "Correo inválido.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient("SoftfyApi");
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
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                ModelState.AddModelError(nameof(dto.NewPassword), "La contraseña debe tener al menos 6 caracteres.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient("SoftfyApi");
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult RegistroArtista() => View();

        [HttpGet]
        public IActionResult Registro() => View(new UsuarioRegistroDto());

        [HttpPost]
        public async Task<IActionResult> Registro(UsuarioRegistroDto dto)
        {
            if (!EsContrasenaSegura(dto.Password))
                ModelState.AddModelError(nameof(dto.Password), "Debe tener ≥6 caracteres, 1 mayúscula y 1 número.");
            if (!IsValidEmail(dto.Email))
                ModelState.AddModelError(nameof(dto.Email), "Correo inválido.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient("SoftfyApi");
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
        public async Task<IActionResult> Login(string returnUrl = null, string msg = null)
        {
            if (msg == "actualizar")
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                ViewBag.Mensaje = "Suscripción activada correctamente. Por favor, vuelve a iniciar sesión.";
            }

            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Info = TempData["RegistroOk"];
            return View(new UsuarioLoginDto());
        }

        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto dto, string returnUrl = null)
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var resp = await client.PostAsJsonAsync("https://localhost:7003/api/auth/login", dto);
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var errorResponse = JsonDocument.Parse(raw).RootElement;
                string errorMessage = "Error desconocido.";

                if (errorResponse.TryGetProperty("error", out var errorProp))
                {
                    errorMessage = errorProp.GetString();
                }
                else if (errorResponse.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in errorsProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                        {
                            errorMessage = prop.Value[0].GetString(); // Toma el primer mensaje de error
                            break;
                        }
                    }
                }
                // Buscar en 'message'
                else if (errorResponse.TryGetProperty("message", out var messageProp))
                {
                    errorMessage = messageProp.GetString();
                }

                ViewBag.Error = errorMessage;
                return View(dto);
            }

            // Si las credenciales son correctas, procesamos el JWT
            var token = JsonDocument.Parse(raw).RootElement.GetProperty("token").GetString();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var identity = new ClaimsIdentity(
                jwtToken.Claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            identity.AddClaim(new Claim("jwt", token));

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

            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            if (role == "Artista")
                return RedirectToAction(nameof(BienvenidoArtista));
            if (role == "Oyente" || role == "OyentePremium")
                return RedirectToAction(nameof(BienvenidoOyente));

            return RedirectToAction(nameof(Bienvenido));
        }




        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
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
            var client = ObtenerClienteConToken();
            string nombreArtistico = User.Identity?.Name ?? "Artista";
            ViewBag.Canciones = new List<CancionRespuestaDto>();
            ViewBag.Playlists = new List<PlaylistDto>();

            var perfil = await client.GetFromJsonAsync<PerfilArtistaDto>("artistas/mi-perfil");
            if (perfil is not null)
                nombreArtistico = perfil.NombreArtistico;

            var canciones = await client.GetFromJsonAsync<List<CancionRespuestaDto>>("canciones/mis-canciones");
            if (canciones is not null)
                ViewBag.Canciones = canciones;

            var playlists = await client.GetFromJsonAsync<List<PlaylistDto>>("playlists/mis-playlists");
            if (playlists is not null)
                ViewBag.Playlists = playlists;

            ViewBag.ArtistaNombre = nombreArtistico;

            return View();
        }

        public async Task<IActionResult> BienvenidoOyente()
        {
            var client = ObtenerClienteConToken();
            string nombreOyente = User.Identity?.Name ?? "Oyente";

            var perfil = await client.GetFromJsonAsync<PerfilOyenteDto>("oyentes/mi-perfil");
            if (perfil is not null)
                nombreOyente = $"{perfil.Nombre} {perfil.Apellido}";

            ViewBag.OyenteNombre = nombreOyente;

            var canciones = await client.GetFromJsonAsync<List<CancionRespuestaDto>>("https://localhost:7003/api/Canciones/canciones");

            canciones = canciones
                .Where(c => !string.IsNullOrWhiteSpace(c.UrlArchivo))
                .ToList();

            var playlists = await client.GetFromJsonAsync<List<PlaylistDto>>("https://localhost:7003/api/Playlists/todas/artistas");
            ViewBag.TodasPlaylists = playlists;

            // Obtener artistas
            var artistas = await client.GetFromJsonAsync<List<Artista>>("https://localhost:7003/api/Artistas");

            foreach (var artista in artistas)
            {
                if (!string.IsNullOrEmpty(artista.FotoUrl))
                    artista.FotoUrl = $"https://localhost:7003/api/artistas/foto/{artista.FotoUrl}";
            }

            ViewBag.OyenteNombre = nombreOyente;
            ViewBag.TodasCanciones = canciones;
            ViewBag.TodasPlaylists = playlists;
            ViewBag.Artistas = artistas;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Bienvenido()
        {
            var client = ObtenerClienteConToken();

            var canciones = await client.GetFromJsonAsync<List<CancionRespuestaDto>>("https://localhost:7003/api/Canciones/canciones");

            canciones = canciones
                .Where(c => !string.IsNullOrWhiteSpace(c.UrlArchivo))
                .ToList();

            ViewBag.TodasCanciones = canciones;
            ViewBag.CantidadCanciones = canciones.Count;

            var playlists = await client.GetFromJsonAsync<List<PlaylistDto>>("https://localhost:7003/api/Playlists/todas/artistas");

            ViewBag.TodasPlaylists = playlists;
            ViewBag.CantidadPlaylists = playlists?.Count ?? 0;

            var artistas = await client.GetFromJsonAsync<List<Artista>>("https://localhost:7003/api/Artistas");

            foreach (var artista in artistas)
            {
                if (!string.IsNullOrEmpty(artista.FotoUrl))
                    artista.FotoUrl = $"https://localhost:7003/api/artistas/foto/{artista.FotoUrl}";
            }

            ViewBag.Artistas = artistas;
            ViewBag.CantidadArtistas = artistas?.Count ?? 0;

            return View();
        }


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
        [AllowAnonymous]
        public async Task<IActionResult> Buscar(string termino)
        {
            if (string.IsNullOrEmpty(termino))
            {
                ViewBag.Error = "Por favor, ingrese un término de búsqueda.";
                return View();
            }

            var client = ObtenerClienteConToken();

            var cancionesResponse = await client.GetAsync($"https://localhost:7003/api/Canciones/por-nombre/{termino}");
            var artistasResponse = await client.GetAsync($"https://localhost:7003/api/Artistas/perfil/{termino}");
            var playlistResponse = await client.GetAsync($"https://localhost:7003/api/Playlists/buscar/{termino}");

            if (cancionesResponse.IsSuccessStatusCode)
            {
                var cancionesJson = await cancionesResponse.Content.ReadAsStringAsync();
                var canciones = JsonSerializer.Deserialize<List<CancionRespuestaDto>>(cancionesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (canciones != null && canciones.Any())
                {
                    ViewBag.Canciones = canciones.Where(c => !string.IsNullOrWhiteSpace(c.UrlArchivo)).ToList();
                }
                else
                {
                    ViewBag.Error = "No se encontraron canciones.";
                }
            }

            // Verificar la respuesta del artista
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

            if (playlistResponse.IsSuccessStatusCode)
            {
                var playlistsJson = await playlistResponse.Content.ReadAsStringAsync();
                var playlists = JsonSerializer.Deserialize<List<PlaylistDto>>(playlistsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var filtradas = playlists
                    .Where(p => !string.Equals(p.Nombre, "Canciones que te gustan", StringComparison.OrdinalIgnoreCase))
                    .ToList();


                if (filtradas.Any())
                {
                    ViewBag.Playlists = filtradas;
                }
                else
                {
                    ViewBag.Error = "No se encontro Albums con este Nombre.";
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
                var perfil = await client.GetFromJsonAsync<PerfilArtistaDto>("https://localhost:7003/api/Artistas/mi-perfil");

                if (perfil != null)
                {
                    ViewBag.TipoUsuario = "Artista";
                    ViewBag.NombreArtistico = perfil.NombreArtistico;
                    ViewBag.FotoUrl = perfil.FotoUrl;
                    ViewBag.Biografia = perfil.Biografia;
                    ViewBag.Email = perfil.UsuarioEmail;
                    return View("VerPerfilArtista");
                }
            }
            else if (rol == "Oyente" || rol == "OyentePremium")
            {
                var perfil = await client.GetFromJsonAsync<PerfilOyenteDto>("https://localhost:7003/api/Oyentes/mi-perfil");

                if (perfil != null)
                {
                    ViewBag.TipoUsuario = perfil.TipoUsuario;
                    ViewBag.Nombre = perfil.Nombre;
                    ViewBag.Apellido = perfil.Apellido;
                    ViewBag.Email = perfil.Email;
                    return View("VerPerfilOyente");
                }
            }

            return NotFound("Perfil no encontrado.");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerPerfilpublico(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var perfil = await client.GetFromJsonAsync<PerfilArtistaDto>($"https://localhost:7003/api/Artistas/{id}");
            if (perfil == null)
                return NotFound("Perfil del artista no encontrado");

            var canciones = await client.GetFromJsonAsync<List<CancionDto>>($"https://localhost:7003/api/Artistas/{id}/canciones");

            if (!string.IsNullOrEmpty(perfil.FotoUrl))
                perfil.FotoUrl = $"https://localhost:7003/api/artistas/foto/{perfil.FotoUrl}";

            ViewBag.ArtistaId = id;
            ViewBag.NombreArtistico = perfil.NombreArtistico;
            ViewBag.FotoUrl = perfil.FotoUrl;
            ViewBag.Biografia = perfil.Biografia;
            ViewBag.Email = perfil.UsuarioEmail;
            ViewBag.Canciones = canciones;

            return View("VerPerfilPublicoArtista");
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarPerfilArtista(string NombreArtistico, string Biografia, IFormFile Foto)
        {
            var client = ObtenerClienteConToken();

            var form = new MultipartFormDataContent
            {
                { new StringContent(NombreArtistico ?? ""), "nombreArtistico" },
                { new StringContent(Biografia ?? ""), "biografia" }
            };

            if (Foto != null && Foto.Length > 0)
            {
                var streamContent = new StreamContent(Foto.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(Foto.ContentType);
                form.Add(streamContent, "foto", Foto.FileName);
            }

            var response = await client.PutAsync("https://localhost:7003/api/artistas/actualizar", form);
            TempData[response.IsSuccessStatusCode ? "Mensaje" : "Error"] =
                response.IsSuccessStatusCode ? "Perfil actualizado correctamente." : "Error al actualizar perfil.";

            return RedirectToAction("VerPerfil");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPerfilOyente(string Nombre, string Apellido)
        {
            if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellido))
            {
                TempData["Error"] = "El nombre y apellido no pueden estar vacíos.";
                return RedirectToAction("VerPerfil");
            }

            var client = ObtenerClienteConToken();
            var content = new StringContent(
                JsonSerializer.Serialize(new { nombre = Nombre, apellido = Apellido }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://localhost:7003/api/oyentes/actualizar", content);
            var mensaje = await response.Content.ReadAsStringAsync();

            TempData[response.IsSuccessStatusCode ? "Mensaje" : "Error"] =
                response.IsSuccessStatusCode ? "Perfil actualizado correctamente." : $"Error al actualizar perfil: {mensaje}";

            return RedirectToAction("VerPerfil");
        }
    }
}