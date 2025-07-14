using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftfyWeb.Modelos.Dtos
{
    public class CancionRespuestaDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string? Genero { get; set; }
        public DateTime FechaLanzamiento { get; set; }
        public string? UrlArchivo { get; set; }
        public double DuracionSegundos { get; set; }
        public double TamanoMB { get; set; }
        public string? Formato { get; set; }
        public string? PublicId { get; set; }

        public ArtistaDto? Artista { get; set; }
    }

}
