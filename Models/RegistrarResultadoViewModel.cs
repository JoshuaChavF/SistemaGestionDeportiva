using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class RegistrarResultadoViewModel
    {
        public int PartidoId { get; set; }

        [Display(Name = "Equipo Local")]
        public string EquipoLocalNombre { get; set; }

        [Display(Name = "Equipo Visitante")]
        public string EquipoVisitanteNombre { get; set; }

        [Required(ErrorMessage = "Los goles del equipo local son requeridos")]
        [Range(0, 50, ErrorMessage = "Los goles deben estar entre 0 y 50")]
        [Display(Name = "Goles Local")]
        public int GolesLocal { get; set; }

        [Required(ErrorMessage = "Los goles del equipo visitante son requeridos")]
        [Range(0, 50, ErrorMessage = "Los goles deben estar entre 0 y 50")]
        [Display(Name = "Goles Visitante")]
        public int GolesVisitante { get; set; }
    }
}