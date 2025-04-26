using System.Collections.Generic;

namespace SistemaGestionDeportiva.Models
{
    public class InformeEquipoViewModel
    {
        public Equipo Equipo { get; set; }
        public int PartidosJugados { get; set; }
        public int PartidosGanados { get; set; }
        public int PartidosEmpatados { get; set; }
        public int PartidosPerdidos { get; set; }
        public int GolesAFavor { get; set; }
        public int GolesEnContra { get; set; }
        public List<JugadorInformeViewModel> JugadoresDestacados { get; set; }
        public List<PartidoInformeViewModel> UltimosPartidos { get; set; }
    }

    public class JugadorInformeViewModel
    {
        public Jugador Jugador { get; set; }
        public int PartidosJugados { get; set; }
        public int Goles { get; set; }
        public int Asistencias { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
    }

    public class PartidoInformeViewModel
    {
        public System.DateTime Fecha { get; set; }
        public string Rival { get; set; }
        public bool EsLocal { get; set; }
        public int GolesFavor { get; set; }
        public int GolesContra { get; set; }
        public string Resultado { get; set; } // G, E, P
    }
}