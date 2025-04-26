using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionDeportiva.Models
{
    public class EstadisticaPartido
    {

        [Key]
        public int EstadisticaId { get; set; }
        [ForeignKey("Jugador")]
        public int JugadorId { get; set; }
        public Jugador Jugador { get; set; }
        [ForeignKey("Partido")]
        public int PartidoId { get; set; }
        public Partido Partido { get; set; }

        public int Goles { get; set; }
        public int Asistencias { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
        public int MinutosJugados { get; set; }
        public string Notas { get; set; }


    }


}