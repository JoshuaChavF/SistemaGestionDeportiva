using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class CrearEquipoViewModel
    {
        // Datos del equipo
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; }

        public string EscudoUrl { get; set; }
        public string ColorPrincipal { get; set; } = "#0000ff";
        public string ColorSecundario { get; set; } = "#ffffff";

        [Required(ErrorMessage = "Debe seleccionar una liga")]
        public int LigaId { get; set; }

        // Lista de jugadores
        public List<JugadorViewModel> Jugadores { get; set; } = new List<JugadorViewModel>();

        // Lista de entrenadores
        public List<EntrenadorViewModel> Entrenadores { get; set; } = new List<EntrenadorViewModel>();

        // Archivo de escudo
        public IFormFile EscudoFile { get; set; }
    }

    public class JugadorViewModel
    {
        [Required(ErrorMessage = "Nombre es obligatorio")]
        public string Nombre { get; set; }

        public string Posicion { get; set; }
        public int NumeroCamiseta { get; set; }
        
    }

    public class EntrenadorViewModel
    {
        [Required(ErrorMessage = "Nombre es obligatorio")]
        public string Nombre { get; set; }

        public string Especialidad { get; set; }
        public string UsuarioId { get; set; }

    }
}
