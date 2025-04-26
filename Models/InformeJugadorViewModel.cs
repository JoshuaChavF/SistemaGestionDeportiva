using System.Collections.Generic;

namespace SistemaGestionDeportiva.Models
{
    public class InformeJugadorViewModel
    {
        public Jugador Jugador { get; set; }
        public int PartidosJugados { get; set; }
        public int Goles { get; set; }
        public int Asistencias { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
        public double PromedioGoles { get; set; }
        public double PromedioAsistencias { get; set; }
        public List<PartidoJugadorViewModel> UltimosPartidos { get; set; }
    }

    public class PartidoJugadorViewModel
    {
        public System.DateTime Fecha { get; set; }
        public string Rival { get; set; }
        public int Goles { get; set; }
        public int Asistencias { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
        public int MinutosJugados { get; set; }
    }
}