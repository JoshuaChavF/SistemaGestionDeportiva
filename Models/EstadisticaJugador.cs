using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SistemaGestionDeportiva.Models
{
    public class EstadisticaJugador
    {
        [Key]
        public int EstadisticaId { get; set; }

        [Required]
        [ForeignKey("Partido")]
        public int PartidoId { get; set; }

        [Required]
        [ForeignKey("Jugador")]
        public int JugadorId { get; set; }

        [Required]
        [ForeignKey("Equipo")]
        public int EquipoId { get; set; }

        public bool TarjetaAmarilla { get; set; }
        public bool TarjetaRoja { get; set; }
        public int? Minuto { get; set; }

        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Partido Partido { get; set; }

        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Jugador Jugador { get; set; }

        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Equipo Equipo { get; set; }
    }
}
