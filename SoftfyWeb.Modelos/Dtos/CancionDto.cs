
namespace SoftfyWeb.Modelos.Dtos
{
    public class CancionDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Genero { get; set; }
        public DateTime FechaLanzamiento { get; set; }
        public string UrlArchivo { get; set; }
    }
}
