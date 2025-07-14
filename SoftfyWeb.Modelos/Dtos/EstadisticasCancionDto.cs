using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftfyWeb.Modelos.Dtos
{
    public class EstadisticasCancionDto
    {
        public string Titulo { get; set; }
        public string Genero { get; set; }
        public DateTime FechaLanzamiento { get; set; }
        public long Bytes { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
