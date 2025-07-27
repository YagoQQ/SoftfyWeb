namespace SoftfyWeb.Modelos.Dtos
{
    public class PlanDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } // Nombre del plan (por ejemplo, "Plan Básico")
        public decimal Precio { get; set; } // Precio del plan
        public int MaxUsuarios { get; set; } // Número máximo de usuarios para este plan
    }
}
