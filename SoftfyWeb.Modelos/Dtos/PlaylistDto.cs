namespace SoftfyWeb.Modelos.Dtos
{
    public class PlaylistDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Propietario { get; set; }
        public string? NombreArtistico { get; set; }
        public string? Apellido { get; set; }
        public int TotalCanciones { get; set; }
        public int? ArtistaId { get; set; }
    }
}