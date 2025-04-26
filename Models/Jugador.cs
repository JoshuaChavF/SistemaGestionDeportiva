using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionDeportiva.Models
{
    public class Jugador
    {
        [Key]
        public int JugadorId { get; set; }
        public string Nombre { get; set; }
        public string Posicion { get; set; }
        public int NumeroCamiseta { get; set; }

        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        
        public int EquipoId { get; set; }
        [ForeignKey("EquipoId")]
        public Equipo Equipo { get; set; }

        public ICollection<Gol> Goles { get; set; }
        public ICollection<EstadisticaPartido> Estadisticas { get; set; }
        public ICollection<AsistenciaEntrenamiento> Asistencias { get; set; }
        public virtual ICollection<EstadisticaJugador> EstadisticasJugadores { get; set; }


    }
}