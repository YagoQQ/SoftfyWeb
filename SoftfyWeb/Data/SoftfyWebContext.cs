using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Modelos;

namespace SoftfyWeb.Data
{
    public class SoftfyWebContext : DbContext
    {
        public SoftfyWebContext (DbContextOptions<SoftfyWebContext> options)
            : base(options)
        {
        }

        public DbSet<SoftfyWeb.Modelos.Plan> Plan { get; set; } = default!;
    }
}
