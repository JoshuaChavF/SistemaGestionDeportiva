using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class AsignarJugadorViewModel
    {
        [Required(ErrorMessage = "El jugador es requerido")]
        public string JugadorId { get; set; }

        [Required(ErrorMessage = "La posición es requerida")]
        [Display(Name = "Posición")]
        public string Posicion { get; set; }

        [Required(ErrorMessage = "El número de camiseta es requerido")]
        [Display(Name = "Número de Camiseta")]
        [Range(1, 99, ErrorMessage = "El número debe estar entre 1 y 99")]
        public int NumeroCamiseta { get; set; }
    }
}