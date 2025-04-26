namespace SistemaGestionDeportiva.Models
{
    public class PartidoSimulacionViewModel
    {
        public int GolesLocal { get; set; }
        public int GolesVisitante { get; set; }
        public int TarjetasAmarillasLocal { get; set; }
        public int TarjetasAmarillasVisitante { get; set; }
        public int TarjetasRojasLocal { get; set; }
        public int TarjetasRojasVisitante { get; set; }
        public int TirosAlArcoLocal { get; set; }
        public int TirosAlArcoVisitante { get; set; }
        public int PosesionLocal { get; set; }
        public int FaltasLocal { get; set; }
        public int FaltasVisitante { get; set; }
        public string Observaciones { get; set; }
        public bool TiempoExtra { get; set; }
        public bool Penales { get; set; }
        public List<int> GoleadoresLocal { get; set; } = new List<int>();
        public List<int> GoleadoresVisitante { get; set; } = new List<int>();
        public List<GoleadorSimulacion> GoleadoresLocales { get; set; }
        public List<GoleadorSimulacion> GoleadoresVisitantes { get; set; }
    }

    public class GoleadorSimulado
    {
        public int Equipo { get; set; } // 0 para local, 1 para visitante
        public int JugadorId { get; set; }
        public string JugadorNombre { get; set; }
        public string Posicion { get; set; }
        public int Minuto { get; set; }
    }
public class GoleadorSimulacion
{
    public int JugadorId { get; set; }
    public int Minuto { get; set; }
    public bool EsPenal { get; set; }
}
}
