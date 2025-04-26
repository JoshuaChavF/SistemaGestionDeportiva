namespace SistemaGestionDeportiva.Models
{
    public class TablaPosicionesViewModel
    {
        public Equipo Equipo { get; set; }
        public int PartidosJugados { get; set; }
        public int PartidosGanados { get; set; }
        public int PartidosEmpatados { get; set; }
        public int PartidosPerdidos { get; set; }
        public int GolesAFavor { get; set; }
        public int GolesEnContra { get; set; }
        public int DiferenciaGoles { get; set; }
        public int Puntos { get; set; }
    }
}