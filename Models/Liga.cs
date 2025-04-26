using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionDeportiva.Models
{
    public class Liga
    {
        [Key]
        public int LigaId { get; set; }

        [Required(ErrorMessage = "El nombre de la liga es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre de la Liga")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Finalización")]
        public DateTime FechaFin { get; set; } = DateTime.Today.AddMonths(3);

        [Display(Name = "Logo de la Liga")]
        [Url(ErrorMessage = "Debe ingresar una URL válida")]
        public string LogoUrl { get; set; } = "https://ejemplo.com/default-logo.png";

        [Display(Name = "Fecha de Creación")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Planificada"; // Planificada, EnCurso, Finalizada, Cancelada

        // Relaciones
        public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
        public ICollection<Partido> Partidos { get; set; } = new List<Partido>();
        public ICollection<Entrenamiento> Entrenamientos { get; set; } = new List<Entrenamiento>();

        // Método para validar fechas
        public bool FechasValidas()
        {
            return FechaFin > FechaInicio;
        }
    }
}