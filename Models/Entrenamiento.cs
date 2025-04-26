using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class Entrenamiento
    {
        [Key]
        public int EntrenamientoId { get; set; }

        public int EquipoId { get; set; }
        public Equipo Equipo { get; set; }

        public int? EntrenadorId { get; set; }
        public Entrenador Entrenador { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; }

        public string Ubicacion { get; set; }
        public string Objetivo { get; set; }
        public string Descripcion { get; set; }
        public string Observaciones { get; set; }
        public int LigaId { get; set; }
        public Liga Liga { get; set; }
        public ICollection<MetricaEntrenamiento> Metricas { get; set; }

        public ICollection<AsistenciaEntrenamiento> Asistencias { get; set; }
    }
    public class AsistenciaEntrenamiento
    {
        public int AsistenciaEntrenamientoId { get; set; }
        public int JugadorId { get; set; }
        public int EntrenamientoId { get; set; }
        public bool Asistio { get; set; }
        public string Notas { get; set; }

        // Relaciones
        public Jugador Jugador { get; set; }
        
        public Entrenamiento Entrenamiento { get; set; }
    }

    public class MetricaEntrenamiento
    {
        public int Id { get; set; }
        public int JugadorId { get; set; }
        public int EntrenamientoId { get; set; }
        public string TipoMetrica { get; set; } // Ej: "Velocidad", "Precisión"
        public decimal Valor { get; set; }
        public string Comentarios { get; set; }

        // Relaciones
        public Jugador Jugador { get; set; }
        public Entrenamiento Entrenamiento { get; set; }
    }
    public class EntrenamientoInputModel
    {
        public int LigaId { get; set; }
        public DateTime FechaHora { get; set; }
        public string Ubicacion { get; set; }
        public string Objetivo { get; set; }
    }
    public class EntrenamientosViewModel
    {
        public Liga LigaSeleccionada { get; set; }
        public List<Equipo> EquiposDeLiga { get; set; } = new List<Equipo>();
        public int? LigaSeleccionadaId { get; set; }  
        public string LigaNombre { get; set; }
        public List<Liga> TodasLigas { get; set; }
        public List<Entrenamiento> Entrenamientos { get; set; }
        public int NumEntrenamientos { get; set; }
        public int NumPartidos { get; set; }
        public List<EventoCalendario> Eventos { get; set; } = new List<EventoCalendario>();

    }
    public class EventoCalendario
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Color { get; set; }
        public string Tipo { get; set; } // "partido" o "entrenamiento"
        public string Objetivo { get; set; }
        public int? EquipoId { get; set; }
        public string EquipoNombre { get; set; }
    }

    public class GenerarEntrenamientosModel
    {
        public int LigaId { get; set; }
        public string FechaInicio { get; set; }
        public string FechaFin { get; set; }
        public int Frecuencia { get; set; } // Días entre entrenamientos
        public string HoraInicio { get; set; }
        public int Duracion { get; set; } // Minutos
    }

}