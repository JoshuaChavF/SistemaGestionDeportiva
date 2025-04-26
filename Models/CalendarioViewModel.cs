using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaGestionDeportiva.Models
{
    public class CalendarioViewModel
    {
        public Liga Liga { get; set; }
        public List<Partido> Partidos { get; set; }
        public int? LigaSeleccionadaId { get; set; }
       
        public int PartidosPorDia { get; set; } = 2;

        public DateTime? FechaFiltro { get; set; }
        public SelectList ListaLigas { get; set; }
        public List<Entrenamiento> Entrenamientos { get; set; } = new List<Entrenamiento>();
        public List<Liga> TodasLigas { get; set; }  
        public ICollection<Partido> Partido { get; set; } = new List<Partido>();
        public int PartidosJugados => Partidos?.Count(p => p.Estado == "Finalizado") ?? 0;
        public int PartidosPendientes => Partidos?.Count(p => p.Estado == "Programado") ?? 0;
        public int PartidosEnCurso => Partidos?.Count(p => p.Estado == "EnCurso") ?? 0;
        // En modelo Equipo
        public ICollection<Partido> PartidosLocal { get; set; } = new List<Partido>();
        public ICollection<Partido> PartidosVisitante { get; set; } = new List<Partido>();
        public string LigaNombre { get; set; }
        public int NumPartidos { get; set; }
        public int NumEntrenamientos { get; set; }
        public string Temporada { get; set; }
        public int LigaId { get; set; }
    }
}
