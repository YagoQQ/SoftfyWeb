namespace SoftfyWeb.Modelos.Dtos
{
    public class CancionMeGustaDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string? UrlArchivo { get; set; }
        public string Artista { get; set; }
    }
}