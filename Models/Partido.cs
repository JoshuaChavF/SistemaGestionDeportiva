using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class Partido
    {

        [Key]
        public int PartidoId { get; set; }

        public int LigaId { get; set; }
        public Liga Liga { get; set; }

        public int EquipoLocalId { get; set; }
        public Equipo EquipoLocal { get; set; }

        public int EquipoVisitanteId { get; set; }
        public Equipo EquipoVisitante { get; set; }

        [Display(Name = "Fecha Programada")]
        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; }

        [Display(Name = "Fecha de Creación")]
        [DataType(DataType.DateTime)]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Modificación")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaModificacion { get; set; }

        public string Ubicacion { get; set; } = "Estadio Principal";
        public int? GolesLocal { get; set; }
        public int? GolesVisitante { get; set; }
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } // "Programado", "EnJuego", "Finalizado", "Cancelado"

        public ICollection<EstadisticaPartido> Estadisticas { get; set; }
        public virtual ICollection<EstadisticaJugador> EstadisticasJugadores { get; set; }

        public int TarjetasAmarillasLocal { get; set; } = 0;
        public int TarjetasRojasLocal { get; set; } = 0;
        public int TarjetasAmarillasVisitante { get; set; } = 0;
        public int TarjetasRojasVisitante { get; set; } = 0;
        public int TirosAlArcoLocal { get; set; } = 0; 
        public int TirosAlArcoVisitante { get; set; } = 0;
        public int PosesionLocal { get; set; } = 0; // Porcentaje
        public int PosesionVisitante { get; set; } = 0;// Porcentaje

        public string Observaciones { get; set; }

        public int FaltasLocal { get; set; }
        public int FaltasVisitante { get; set; }
        public bool TiempoExtra { get; set; } = false;
        public bool Penales { get; set; } = false;


        public ICollection<Gol> Goles { get; set; }

        public class PartidoViewModel
        {
            public int PartidoId { get; set; }

            [Required]
            public int GolesLocal { get; set; }

            [Required]
            public int GolesVisitante { get; set; }

            public int TarjetasAmarillasLocal { get; set; }
            public int TarjetasRojasLocal { get; set; }
            public int TarjetasAmarillasVisitante { get; set; }
            public int TarjetasRojasVisitante { get; set; }
            public int TirosAlArcoLocal { get; set; }
            public int TirosAlArcoVisitante { get; set; }

            [Range(0, 100)]
            public int PosesionLocal { get; set; }

            public string Observaciones { get; set; }
            public bool Finalizar { get; set; }

           
        }

        public class PosicionViewModel
        {
            public Equipo Equipo { get; set; }
            public int PJ { get; set; } // Partidos Jugados
            public int PG { get; set; } // Partidos Ganados
            public int PE { get; set; } // Partidos Empatados
            public int PP { get; set; } // Partidos Perdidos
            public int GF { get; set; } // Goles a Favor
            public int GC { get; set; } // Goles en Contra
            public int DG { get; set; } // Diferencia de Goles
            public int Puntos { get; set; }
            public List<char> UltimosResultados { get; set; } = new List<char>(); // W, D, L
        }
    }
}