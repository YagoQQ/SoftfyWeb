namespace SoftfyWeb.Modelos.Dtos
{
    public class MiembroSuscripcionDto
    {
        public string UsuarioId { get; set; } // ID del usuario miembro
        public DateTime FechaAgregado { get; set; } // Fecha de agregación del miembro a la suscripción
    }
}
