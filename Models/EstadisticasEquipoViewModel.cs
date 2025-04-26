using System.Collections.Generic;

namespace SistemaGestionDeportiva.Models
{
    public class EstadisticasEquipoViewModel
    {
        public Equipo Equipo { get; set; }
        public int GolesAFavor { get; set; }
        public int GolesEnContra { get; set; }
        public List<Partido> Partidos { get; set; }
        public List<JugadorDestacadoViewModel> JugadoresDestacados { get; set; }
    }

    public class JugadorDestacadoViewModel
    {
        public Jugador Jugador { get; set; }
        public int TotalGoles { get; set; }
        public int TotalAsistencias { get; set; }
    }
}