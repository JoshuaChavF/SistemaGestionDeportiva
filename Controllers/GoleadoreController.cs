using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;

namespace SistemaGestionDeportiva.Controllers
{
    public class GoleadoreController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartidosController> _logger;

        public GoleadoreController(ApplicationDbContext context, ILogger<PartidosController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int? ligaId)
        {
            // Obtener todas las ligas
            var ligas = await _context.Ligas.ToListAsync();
            ViewBag.Ligas = new SelectList(ligas, "LigaId", "Nombre", ligaId);

            var viewModel = new GoleadoresLigaViewModel();

            if (ligaId.HasValue)
            {
                // Verificar si la liga existe
                var liga = await _context.Ligas.FindAsync(ligaId.Value);
                if (liga == null)
                {
                    return NotFound();
                }

                viewModel.LigaId = ligaId.Value;
                viewModel.NombreLiga = liga.Nombre;

                // Consulta optimizada para goleadores
                viewModel.Goleadores = await _context.Goles
                    .Where(g => g.Partido.LigaId == ligaId.Value && !g.EsAutogol) // Excluir autogoles
                    .GroupBy(g => new {
                        g.JugadorId,
                        JugadorNombre = g.Jugador.Nombre,
                        Posicion = g.Jugador.Posicion,
                        EquipoNombre = g.Equipo.Nombre,
                        EscudoEquipo = g.Equipo.EscudoUrl
                    })
                    .Select(g => new Goleador
                    {
                        JugadorId = g.Key.JugadorId,
                        NombreJugador = g.Key.JugadorNombre,
                        Posicion = g.Key.Posicion,
                        EquipoNombre = g.Key.EquipoNombre,
                        EscudoEquipo = g.Key.EscudoEquipo,
                        Goles = g.Count(),
                        PartidosJugados = _context.Partidos
                            .Count(p => p.LigaId == ligaId.Value &&
                                  (p.EquipoLocalId == g.First().EquipoId ||
                                   p.EquipoVisitanteId == g.First().EquipoId) &&
                                  p.Estado == "Finalizado")
                    })
                    .OrderByDescending(g => g.Goles)
                    .ThenBy(g => g.NombreJugador)
                    .ToListAsync();

                // Debug: Verificar datos
                Console.WriteLine($"Goleadores encontrados: {viewModel.Goleadores.Count}");
                foreach (var g in viewModel.Goleadores)
                {
                    Console.WriteLine($"{g.NombreJugador} - {g.Goles} goles");
                }
            }

            return View(viewModel);
        }
    }
}
