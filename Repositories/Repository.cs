using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System.Globalization;

namespace SistemaGestionDeportiva.Repositories
{
    public class Repository : IRepository
    {
        private readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<JugadorEstadisticas> ObtenerEstadisticasJugadores()
        {
            return _context.JugadoresEstadisticas.ToList();
        }

        public EquipoEstadisticas ObtenerEstadisticasEquipo()
        {
            return _context.EquiposEstadisticas.FirstOrDefault() ?? new EquipoEstadisticas();
        }
        public List<Tendencia> ObtenerTendencias(int? ligaId, int? equipoId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                var query = _context.Partidos.AsQueryable();

                if (ligaId.HasValue)
                    query = query.Where(p => p.LigaId == ligaId);

                if (equipoId.HasValue)
                    query = query.Where(p => p.EquipoLocalId == equipoId || p.EquipoVisitanteId == equipoId);

                if (fechaInicio.HasValue)
                    query = query.Where(p => p.FechaHora >= fechaInicio);

                if (fechaFin.HasValue)
                    query = query.Where(p => p.FechaHora <= fechaFin);

                var tendencias = query
                    .GroupBy(p => new { p.FechaHora.Year, p.FechaHora.Month })
                    .Select(g => new Tendencia
                    {
                        Periodo = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                        Goles = g.Sum(p => (p.GolesLocal ?? 0) + (p.GolesVisitante ?? 0)),
                        Victorias = g.Count(p => (p.EquipoLocalId == equipoId && p.GolesLocal > p.GolesVisitante) ||
                                                (p.EquipoVisitanteId == equipoId && p.GolesVisitante > p.GolesLocal))
                    })
                    .OrderBy(t => t.Periodo)
                    .ToList();

                return tendencias ?? new List<Tendencia>();
            }
            catch
            {
                return new List<Tendencia>();
            }
        }
    }
}