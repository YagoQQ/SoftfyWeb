namespace SoftfyWeb.Modelos.Dtos
{
    public class SuscripcionDto
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public string UsuarioPrincipalId { get; set; } // ID del titular de la suscripción
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string PlanNombre { get; set; } // Nombre del plan (por ejemplo, "Plan Básico")
        public decimal Precio { get; set; } // Precio del plan
        public List<MiembroSuscripcionDto> Miembros { get; set; } // Miembros asociados a esta suscripción
    }
}
