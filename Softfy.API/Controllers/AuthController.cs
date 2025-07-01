using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SoftfyWeb.Data;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Services;
using System.Text;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;

        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            RoleManager<IdentityRole> roleManager,
            JwtService jwtService,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        [Authorize]
        [HttpGet("protegido")]
        public IActionResult SoloUsuariosLogueados()
        {
            var nombreUsuario = User.Identity?.Name;
            return Ok(new
            {
                mensaje = "¡Hola, estás autenticado!",
                usuario = nombreUsuario
            });
        }

        [HttpPost("registro")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto dto)
        {
            // Validación previa de email
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest(new { error = "Ya existe una cuenta registrada con este correo." });

            // Crear usuario
            var user = new Usuario
            {
                UserName = dto.Email,
                Email = dto.Email,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                TipoUsuario = dto.TipoUsuario
            };

            var resultado = await _userManager.CreateAsync(user, dto.Password);
            if (!resultado.Succeeded)
            {
                // Capturar error de email duplicado por si la validación previa falló
                if (resultado.Errors.Any(e => e.Code == "DuplicateEmail"))
                    return BadRequest(new { error = "El correo ya está en uso." });

                return BadRequest(resultado.Errors);
            }

            // Crear rol si no existe y asignarlo
            if (!await _roleManager.RoleExistsAsync(dto.TipoUsuario))
                await _roleManager.CreateAsync(new IdentityRole(dto.TipoUsuario));

            await _userManager.AddToRoleAsync(user, dto.TipoUsuario);

            // Generar token y link de confirmación
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var linkConfirmacion = Url.Action("ConfirmEmail", "Auth",
                new { userId = user.Id, token }, Request.Scheme);

            await _emailService.EnviarEmailAsync(dto.Email, "Confirma tu cuenta",
                $"Por favor confirma tu cuenta haciendo clic en este enlace: {linkConfirmacion}");

            return Ok(new { mensaje = "Usuario registrado correctamente. Se ha enviado el correo de confirmación." });
        }

        [HttpGet("confirmar-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null)
                return BadRequest("Usuario inválido");

            var resultado = await _userManager.ConfirmEmailAsync(usuario, token);
            if (resultado.Succeeded)
                return Ok("Correo confirmado exitosamente");

            return BadRequest("Error al confirmar el correo");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginDto dto)
        {
            var usuario = await _userManager.FindByEmailAsync(dto.Email);

            // Verificar si el usuario existe
            if (usuario == null)
                return Unauthorized(new { error = "Credenciales inválidas" });

            // Verificar si el correo está confirmado
            if (!usuario.EmailConfirmed)
            {
                return Unauthorized(new { error = "Debes confirmar tu correo antes de iniciar sesión." });
            }

            // Verificar si la cuenta está bloqueada
            if (await _userManager.IsLockedOutAsync(usuario))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(usuario);
                var remainingLockoutTime = lockoutEnd?.Subtract(DateTimeOffset.Now);

                return Unauthorized(new { error = $"La cuenta está bloqueada. Intenta nuevamente en {remainingLockoutTime?.Minutes} minutos." });
            }

            // Validar las credenciales
            var resultado = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, false);

            if (!resultado.Succeeded)
            {
                // Incrementar el contador de intentos fallidos
                await _userManager.AccessFailedAsync(usuario);

                // Verificar si la cuenta está bloqueada después del fallo
                if (await _userManager.IsLockedOutAsync(usuario))
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(usuario);
                    var remainingLockoutTime = lockoutEnd?.Subtract(DateTimeOffset.Now);

                    return Unauthorized(new { error = $"La cuenta está bloqueada. Intenta nuevamente en {remainingLockoutTime?.Minutes} minutos." });
                }

                return Unauthorized(new { error = "Credenciales inválidas" });
            }

            // Si el login es exitoso, restablecer los intentos fallidos
            await _userManager.ResetAccessFailedCountAsync(usuario);

            // Si el login es exitoso, generar el token JWT y devolverlo
            var roles = await _userManager.GetRolesAsync(usuario);
            var token = _jwtService.GenerarToken(usuario, roles);

            return Ok(new
            {
                mensaje = "Login exitoso",
                token = token
            });
        }

        [HttpPost("registro-artista")]
        public async Task<IActionResult> RegistrarArtista([FromBody] ArtistaRegistroDto dto)
        {
            // 1) Validación previa de email
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest(new { error = "Ya existe una cuenta registrada con este correo." });

            // 2) Crear usuario Artista
            var user = new Usuario
            {
                UserName = dto.Email,
                Email = dto.Email,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                TipoUsuario = "Artista"
            };

            var resultado = await _userManager.CreateAsync(user, dto.Password);
            if (!resultado.Succeeded)
            {
                if (resultado.Errors.Any(e => e.Code == "DuplicateEmail"))
                    return BadRequest(new { error = "El correo ya está en uso." });

                return BadRequest(resultado.Errors);
            }

            // 3) Crear rol "Artista" si no existe y asignarlo
            if (!await _roleManager.RoleExistsAsync("Artista"))
                await _roleManager.CreateAsync(new IdentityRole("Artista"));

            await _userManager.AddToRoleAsync(user, "Artista");

            // 4) Crear perfil de artista
            var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var artista = new Artista
            {
                UsuarioId = user.Id,
                NombreArtistico = dto.NombreArtistico,
                Biografia = dto.Biografia,
                FotoUrl = dto.FotoUrl
            };
            context.Artistas.Add(artista);
            await context.SaveChangesAsync();

            // 5) Generar token de confirmación del correo
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var linkConfirmacion = Url.Action("ConfirmEmail", "Auth", new { userId = user.Id, token }, Request.Scheme);

            // 6) Enviar correo de confirmación
            await _emailService.EnviarEmailAsync(dto.Email, "Confirma tu cuenta",
                $"Por favor confirma tu cuenta haciendo clic en este enlace: {linkConfirmacion}");

            return Ok(new { mensaje = "Artista registrado correctamente. Se ha enviado el correo de confirmación." });
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                return Ok(new { mensaje = "Si el correo existe recibirás instrucciones para restablecer tu contraseña." });

            // Generar el token de restablecimiento de contraseña
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Crear el enlace manualmente
            var frontendUrl = "https://localhost:7130";  // Asegúrate de usar la URL correcta de tu frontend
            var resetLink = $"{frontendUrl}/VistasAuth/ResetPassword?userId={Uri.EscapeDataString(user.Id)}" +
                            $"&token={Uri.EscapeDataString(encodedToken)}&email={Uri.EscapeDataString(dto.Email)}";

            // Usar tu EmailService para enviar el correo con el enlace de restablecimiento
            await _emailService.EnviarEmailAsync(
                dto.Email,                  // El correo del usuario
                "Recupera tu contraseña",   // Asunto del correo
                $"Para restablecer tu contraseña, haz clic aquí: {resetLink}"  // Cuerpo del correo
            );

            return Ok(new { mensaje = "Si el correo existe recibirás instrucciones para restablecer tu contraseña." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest(new { error = "Usuario inválido." });

            // Decodificar el token
            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
            }
            catch
            {
                return BadRequest(new { error = "Token inválido." });
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errores = result.Errors.Select(e => e.Description);
                return BadRequest(errores);
            }

            return Ok(new { mensaje = "Contraseña restablecida correctamente." });
        }
    }
}
