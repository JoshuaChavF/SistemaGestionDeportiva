using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class Equipo
    {
        [Key]
        public int EquipoId { get; set; }

        [Required]
        public string Nombre { get; set; }

        public string EscudoUrl { get; set; }
        public string ColorPrincipal { get; set; }
        public string ColorSecundario { get; set; }

        public int LigaId { get; set; }
        public Liga Liga { get; set; }

        public ICollection<Gol> Goles { get; set; }
        public ICollection<Jugador> Jugadores { get; set; }
        public ICollection<Entrenador> Entrenadores { get; set; }
        public ICollection<Partido> PartidosLocal { get; set; }
        public ICollection<Partido> PartidosVisitante { get; set; }
        public virtual ICollection<EstadisticaJugador> EstadisticasJugadores { get; set; }

        public int PartidosJugados { get; set; }
        public int PartidosGanados { get; set; }
        public int PartidosEmpatados { get; set; }
        public int PartidosPerdidos { get; set; }
        public int GolesAFavor { get; set; }
        public int GolesEnContra { get; set; }
        public int DiferenciaGoles => GolesAFavor - GolesEnContra;
        public int Puntos => (PartidosGanados * 3) + (PartidosEmpatados * 1);
    }
}