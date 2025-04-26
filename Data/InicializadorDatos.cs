using Microsoft.AspNetCore.Identity;
using SistemaGestionDeportiva.Models;

namespace SistemaGestionDeportiva.Data
{
    public static class InicializadorDatos
    {
        public static async Task InicializarAsync(UserManager<Usuario> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            // Crear roles si no existen
            string[] roles = { "Administrador", "Entrenador", "Jugador" };

            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            // Crear usuario administrador si no existe
            string adminEmail = "admin@sistema.com";
            string adminPassword = "Admin123!";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new Usuario
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    NombreCompleto = "Administrador del Sistema",
                    FechaNacimiento = new DateTime(1980, 1, 1),
                    TipoUsuario = "Administrador",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }
            }

            // Agregar datos iniciales de prueba si la base de datos está vacía
            if (!context.Ligas.Any())
            {
                var liga = new Liga
                {
                    Nombre = "Liga Principal",
                    Descripcion = "Liga principal de la temporada 2023",
                    FechaInicio = DateTime.Now,
                    FechaFin = DateTime.Now.AddMonths(6)
                };

                context.Ligas.Add(liga);
                await context.SaveChangesAsync();
            }
        }
    }
}