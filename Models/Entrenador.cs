using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionDeportiva.Models
{
    public class Entrenador
    {
        [Key]
        public int EntrenadorId { get; set; }
        public string Nombre { get; set; }
        public string? UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
        public int EquipoId { get; set; }
        [ForeignKey("EquipoId")]
        public Equipo Equipo { get; set; }

        public string Especialidad { get; set; }

        public ICollection<Entrenamiento> Entrenamientos { get; set; }
    }
}