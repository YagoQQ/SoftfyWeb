using Microsoft.AspNetCore.Http;

namespace SoftfyWeb.Modelos.Dtos
{
    public class CancionEditarDto
    {
        public int Id { get; set; }  // Agregar la propiedad Id
        public string Titulo { get; set; }
        public string Genero { get; set; }
        public DateTime? FechaLanzamiento { get; set; }
        public IFormFile ArchivoCancion { get; set; }
        public string UrlArchivo { get; set; } // Para mostrar la URL actual del archivo si es necesario
    }


}
