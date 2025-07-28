using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SoftfyWeb.Modelos;
using System;
using System.Threading.Tasks;

namespace SoftfyWeb.Data
{
    public static class SeedData
    {
        private static readonly string[] roles = new[] { "Admin", "OyenteFree", "OyentePremium", "Artista" };

        public static async Task InicializarRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }
        }
        public static async Task CrearUsuarioInicialAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string email = "admin@softfy.com";
            string password = "Admin123!";

            // Verifica si ya existe
            var usuario = await userManager.FindByEmailAsync(email);
            if (usuario == null)
            {
                usuario = new Usuario
                {
                    UserName = email,
                    Email = email,
                    Nombre = "Super",
                    Apellido = "Admin",
                    TipoUsuario = "Admin",
                    EmailConfirmed = true
                };

                var resultado = await userManager.CreateAsync(usuario, password);
                if (resultado.Succeeded)
                {
                    // Asignar rol
                    await userManager.AddToRoleAsync(usuario, "Admin");
                }
                else
                {
                    // Mostrar errores (opcional)
                    foreach (var error in resultado.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }
            }
        }


    }
}
