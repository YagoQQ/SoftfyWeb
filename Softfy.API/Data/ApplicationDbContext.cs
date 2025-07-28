using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SoftfyWeb.Modelos;

namespace SoftfyWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Artista> Artistas { get; set; }
        public DbSet<Cancion> Canciones { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistCancion> PlaylistCanciones { get; set; }
        public DbSet<Suscripcion> Suscripciones { get; set; }
        public DbSet<Plan> Planes { get; set; }
        public DbSet<MiembroSuscripcion> MiembrosSuscripciones { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PlaylistCancion>()
                .HasKey(pc => new { pc.PlaylistId, pc.CancionId });

            builder.Entity<PlaylistCancion>()
                .HasOne(pc => pc.Playlist)
                .WithMany(p => p.PlaylistCanciones)
                .HasForeignKey(pc => pc.PlaylistId);

            builder.Entity<PlaylistCancion>()
                .HasOne(pc => pc.Cancion)
                .WithMany()
                .HasForeignKey(pc => pc.CancionId);
            // Índice único en Email
            builder.Entity<Usuario>()
                   .HasIndex(u => u.Email)
                   .IsUnique();
        }
    }
}
