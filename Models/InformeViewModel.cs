using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class InformeViewModel
    {
        public int? LigaId { get; set; }
        public int? EquipoId { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // Resultados
        public List<JugadorEstadisticas> EstadisticasJugadores { get; set; }
        public EquipoEstadisticas EstadisticasEquipo { get; set; }
        public List<Tendencia> Tendencias { get; set; }
    }
    public class JugadorEstadisticas
    {
        [Key] // Define esta propiedad como clave primaria
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-incremental
        public int JugadorEstadisticasId { get; set; }
        public string Nombre { get; set; }
        public int? Goles { get; set; }
        public int? Asistencias { get; set; }
        public int? TarjetasAmarillas { get; set; }
        public int? TarjetasRojas { get; set; }
        public int? PartidosJugados { get; set; }
        public double? PromedioGoles { get; set; }
    }

    public class EquipoEstadisticas
    {
        [Key] // Define esta propiedad como clave primaria
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-incremental
        public int EquipoEstadisticasId { get; set; }
        public string Nombre { get; set; }
        public int? PartidosGanados { get; set; }
        public int? PartidosEmpatados { get; set; }
        public int? PartidosPerdidos { get; set; }
        public int? GolesAFavor { get; set; }
        public int? GolesEnContra { get; set; }
        public int TotalEquipos { get; set; } = 0;
    }

    public class Tendencia
    {
        public string Periodo { get; set; }
        public int Goles { get; set; }
        public int Victorias { get; set; }
    }
}
