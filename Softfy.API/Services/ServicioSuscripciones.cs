using Microsoft.AspNetCore.Identity;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;

namespace Softfy.API.Services
{
    public class ServicioSuscripciones : IServicioSuscripciones
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public ServicioSuscripciones(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> ActivarSuscripcionPorEmail(int planId, string email)
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null) return false;

            var plan = await _context.Planes.FindAsync(planId);
            if (plan == null) return false;

            if (_context.Suscripciones.Any(s => s.UsuarioPrincipalId == usuario.Id))
                return false;

            var suscripcion = new Suscripcion
            {
                PlanId = plan.Id,
                UsuarioPrincipalId = usuario.Id,
                FechaInicio = DateTime.UtcNow,
                FechaFin = DateTime.UtcNow.AddMonths(1),
            };

            _context.Suscripciones.Add(suscripcion);
            await _context.SaveChangesAsync();

            var miembro = new MiembroSuscripcion
            {
                UsuarioId = usuario.Id,
                SuscripcionId = suscripcion.Id,
                FechaAgregado = DateTime.UtcNow
            };

            _context.MiembrosSuscripciones.Add(miembro);
            await _context.SaveChangesAsync();

            await _userManager.RemoveFromRoleAsync(usuario, "Oyente");
            await _userManager.AddToRoleAsync(usuario, "OyentePremium");

            usuario.TipoUsuario = "OyentePremium";
            await _userManager.UpdateAsync(usuario);

            return true;
        }
    }
}
