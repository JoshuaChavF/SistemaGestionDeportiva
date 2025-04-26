using Microsoft.AspNetCore.Identity;

namespace SistemaGestionDeportiva.Models
{
    public class Usuario : IdentityUser
    {
        public string NombreCompleto { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string TipoUsuario { get; set; } // "Administrador", "Entrenador", "Jugador"
    }
}