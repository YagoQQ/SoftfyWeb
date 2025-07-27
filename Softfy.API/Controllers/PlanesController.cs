using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;

namespace SoftfyWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlanesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PlanesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtener todos los planes disponibles
        [HttpGet]
        public async Task<IActionResult> ObtenerPlanes()
        {
            var planes = await _context.Planes
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Precio,
                    p.MaxUsuarios
                })
                .ToListAsync();

            return Ok(planes);
        }

        // Registrar un nuevo plan (solo Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPlan([FromBody] Plan plan)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Planes.Add(plan);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Plan registrado correctamente", plan.Id });
        }

        // (Opcional) Actualizar un plan
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPlan(int id, [FromBody] Plan planActualizado)
        {
            var plan = await _context.Planes.FindAsync(id);
            if (plan == null)
                return NotFound(new { mensaje = "Plan no encontrado" });

            plan.Nombre = planActualizado.Nombre;
            plan.Precio = planActualizado.Precio;
            plan.MaxUsuarios = planActualizado.MaxUsuarios;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Plan actualizado correctamente" });
        }

        // (Opcional) Eliminar un plan
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarPlan(int id)
        {
            var plan = await _context.Planes.FindAsync(id);
            if (plan == null)
                return NotFound(new { mensaje = "Plan no encontrado" });

            _context.Planes.Remove(plan);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Plan eliminado correctamente" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPlanPorId(int id)
        {
            var plan = await _context.Planes
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Precio,
                    p.MaxUsuarios
                })
                .FirstOrDefaultAsync();

            if (plan == null)
                return NotFound(new { mensaje = "Plan no encontrado" });

            return Ok(plan);
        }
    }
}
