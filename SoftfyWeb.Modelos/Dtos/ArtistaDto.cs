using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftfyWeb.Modelos.Dtos
{
    public class ArtistaDto
    {
        public int Id { get; set; }
        public string NombreArtistico { get; set; }
        public string? Biografia { get; set; }
        public string? FotoUrl { get; set; }
    }
}
