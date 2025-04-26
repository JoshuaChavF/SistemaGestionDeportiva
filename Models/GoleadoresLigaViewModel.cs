namespace SistemaGestionDeportiva.Models
{
    public class GoleadoresLigaViewModel
    {
        public int LigaId { get; set; }
        public string NombreLiga { get; set; }
        public List<Goleador> Goleadores { get; set; } = new List<Goleador>();

        public bool IncluyeDatosObservaciones { get; set; }
        public int GolesDeObservaciones { get; set; }
    }

    public class Goleador
    {
        public int JugadorId { get; set; }
        public string NombreJugador { get; set; }
        public string EquipoNombre { get; set; }
        public string EscudoEquipo { get; set; }
        public string Posicion { get; set; }
        public int Goles { get; set; }
        public int? PartidoId { get; set; }  
        public int? LigaId { get; set; }     
        public int? PartidosJugados { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
        public decimal PromedioGoles { get; set; }
        public string PromedioFormateado => PromedioGoles.ToString("0.00");
    }
}
