using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionDeportiva.Models
{
    public class Gol
    {

        public int GolId { get; set; }
        public int PartidoId { get; set; }
        public int JugadorId { get; set; }
        public int EquipoId { get; set; }
        public int Minuto { get; set; }
        public bool EsAutogol { get; set; }
        public bool EsPenal { get; set; }

        [ForeignKey("PartidoId")]
        public Partido Partido { get; set; }

        [ForeignKey("JugadorId")]
        public Jugador Jugador { get; set; }

        [ForeignKey("EquipoId")]
        public Equipo Equipo { get; set; }
    }
}
