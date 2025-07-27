using System.Text.Json.Serialization;

namespace SoftfyWeb.Modelos.Dtos
{
    public class SuscripcionEstadoDto
    {
        [JsonPropertyName("tipo")]
        public string Tipo { get; set; }

        [JsonPropertyName("plan")]
        public string Plan { get; set; }

        [JsonPropertyName("precio")]
        public decimal Precio { get; set; }

        // Aquí es donde mapeas "inicio" → FechaInicio
        [JsonPropertyName("inicio")]
        public DateTime FechaInicio { get; set; }

        // Y "fin" → FechaFin
        [JsonPropertyName("fin")]
        public DateTime FechaFin { get; set; }

        [JsonPropertyName("esTitular")]
        public bool EsTitular { get; set; }

        [JsonPropertyName("miembros")]
        public List<MiembroDto> Miembros { get; set; } = new();
    }
}
