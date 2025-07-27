using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftfyWeb.Modelos.Dtos
{
    public class MiembroDto
    {
        public string Email { get; set; }
        public DateTime FechaAgregado { get; set; }
        public bool EsTitular { get; set; }
    }
}
