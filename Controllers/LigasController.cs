using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionDeportiva.Controllers
{
    [Authorize(Roles = "Administrador,Entrenador")]
    public class LigasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LigasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ligas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Ligas
                .Include(l => l.Equipos)
                .Include(l => l.Partidos)
                .OrderByDescending(l => l.FechaCreacion)
                .ToListAsync());
        }
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View(new Liga());
        }
        // GET: Ligas/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var liga = await _context.Ligas
                .Include(l => l.Equipos)
                .Include(l => l.Partidos)
                .FirstOrDefaultAsync(m => m.LigaId == id);

            if (liga == null)
            {
                return NotFound();
            }

            return View(liga);
        }
        // POST: Ligas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("LigaId,Nombre,Descripcion,FechaInicio,FechaFin,LogoUrl")] Liga liga)
        {
            if (!liga.FechasValidas())
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio");
            }

            if (ModelState.IsValid)
            {
                _context.Add(liga);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(liga);
        }

        // GET: Ligas/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var liga = await _context.Ligas.FindAsync(id);
            if (liga == null)
            {
                return NotFound();
            }
            return View(liga);
        }

        // POST: Ligas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("LigaId,Nombre,Descripcion,FechaInicio,FechaFin,LogoUrl,Estado")] Liga liga)
        {
            if (id != liga.LigaId)
            {
                return NotFound();
            }

            if (!liga.FechasValidas())
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(liga);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LigaExists(liga.LigaId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(liga);
        }

        // GET: Ligas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var liga = await _context.Ligas.FindAsync(id);
            _context.Ligas.Remove(liga);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /*// GET: Ligas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var liga = await _context.Ligas
                .Include(l => l.Equipos)
                .Include(l => l.Partidos)
                .FirstOrDefaultAsync(m => m.LigaId == id);

            if (liga == null)
            {
                return NotFound();
            }

            return View(liga);
        }*/

        private bool LigaExists(int id)
        {
            return _context.Ligas.Any(e => e.LigaId == id);
        }
    

        // GET: Ligas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var liga = await _context.Ligas
                .FirstOrDefaultAsync(m => m.LigaId == id);
            if (liga == null)
            {
                return NotFound();
            }

            return View(liga);
        }

        /*// POST: Ligas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var liga = await _context.Ligas.FindAsync(id);
            _context.Ligas.Remove(liga);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LigaExists(int id)
        {
            return _context.Ligas.Any(e => e.LigaId == id);
        }*/
        // GET: Ligas/TablaPosiciones/5
        [AllowAnonymous]
        public async Task<IActionResult> TablaPosiciones(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var liga = await _context.Ligas
                .Include(l => l.Equipos)
                .Include(l => l.Partidos)
                    .ThenInclude(p => p.Estadisticas)
                .FirstOrDefaultAsync(m => m.LigaId == id);

            if (liga == null)
            {
                return NotFound();
            }

            // Calcular tabla de posiciones
            var tabla = liga.Equipos.Select(e => new
            {
                Equipo = e,
                PartidosJugados = liga.Partidos.Count(p =>
                    (p.EquipoLocalId == e.EquipoId || p.EquipoVisitanteId == e.EquipoId) &&
                    p.Estado == "Finalizado"),
                PartidosGanados = liga.Partidos.Count(p =>
                    ((p.EquipoLocalId == e.EquipoId && p.GolesLocal > p.GolesVisitante) ||
                     (p.EquipoVisitanteId == e.EquipoId && p.GolesVisitante > p.GolesLocal)) &&
                    p.Estado == "Finalizado"),
                PartidosEmpatados = liga.Partidos.Count(p =>
                    (p.EquipoLocalId == e.EquipoId || p.EquipoVisitanteId == e.EquipoId) &&
                    p.GolesLocal == p.GolesVisitante &&
                    p.Estado == "Finalizado"),
                PartidosPerdidos = liga.Partidos.Count(p =>
                    ((p.EquipoLocalId == e.EquipoId && p.GolesLocal < p.GolesVisitante) ||
                     (p.EquipoVisitanteId == e.EquipoId && p.GolesVisitante < p.GolesLocal)) &&
                    p.Estado == "Finalizado"),
                GolesAFavor = liga.Partidos
                    .Where(p => p.EquipoLocalId == e.EquipoId && p.Estado == "Finalizado")
                    .Sum(p => p.GolesLocal ?? 0) +
                    liga.Partidos
                    .Where(p => p.EquipoVisitanteId == e.EquipoId && p.Estado == "Finalizado")
                    .Sum(p => p.GolesVisitante ?? 0),
                GolesEnContra = liga.Partidos
                    .Where(p => p.EquipoLocalId == e.EquipoId && p.Estado == "Finalizado")
                    .Sum(p => p.GolesVisitante ?? 0) +
                    liga.Partidos
                    .Where(p => p.EquipoVisitanteId == e.EquipoId && p.Estado == "Finalizado")
                    .Sum(p => p.GolesLocal ?? 0)
            })
            .Select(e => new TablaPosicionesViewModel
            {
                Equipo = e.Equipo,
                PartidosJugados = e.PartidosJugados,
                PartidosGanados = e.PartidosGanados,
                PartidosEmpatados = e.PartidosEmpatados,
                PartidosPerdidos = e.PartidosPerdidos,
                GolesAFavor = e.GolesAFavor,
                GolesEnContra = e.GolesEnContra,
                DiferenciaGoles = e.GolesAFavor - e.GolesEnContra,
                Puntos = (e.PartidosGanados * 3) + (e.PartidosEmpatados * 1)
            })
            .OrderByDescending(e => e.Puntos)
            .ThenByDescending(e => e.DiferenciaGoles)
            .ThenByDescending(e => e.GolesAFavor)
            .ToList();

            ViewData["LigaNombre"] = liga.Nombre;
            return View(tabla);
        }

        // GET: Ligas/EstadisticasEquipo/5
        [AllowAnonymous]
        public async Task<IActionResult> EstadisticasEquipo(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipo = await _context.Equipos
                .Include(e => e.Liga)
                .Include(e => e.PartidosLocal)
                    .ThenInclude(p => p.EquipoVisitante)
                .Include(e => e.PartidosVisitante)
                    .ThenInclude(p => p.EquipoLocal)
                .Include(e => e.Jugadores)
                    .ThenInclude(j => j.Estadisticas)
                        .ThenInclude(es => es.Partido)
                .FirstOrDefaultAsync(e => e.EquipoId == id);

            if (equipo == null)
            {
                return NotFound();
            }

            var partidos = equipo.PartidosLocal
                .Concat(equipo.PartidosVisitante)
                .Where(p => p.Estado == "Finalizado")
                .OrderByDescending(p => p.FechaHora)
                .ToList();

            var viewModel = new EstadisticasEquipoViewModel
            {
                Equipo = equipo,
                Partidos = partidos,
                GolesAFavor = partidos
                    .Where(p => p.EquipoLocalId == equipo.EquipoId)
                    .Sum(p => p.GolesLocal ?? 0) +
                    partidos
                    .Where(p => p.EquipoVisitanteId == equipo.EquipoId)
                    .Sum(p => p.GolesVisitante ?? 0),
                GolesEnContra = partidos
                    .Where(p => p.EquipoLocalId == equipo.EquipoId)
                    .Sum(p => p.GolesVisitante ?? 0) +
                    partidos
                    .Where(p => p.EquipoVisitanteId == equipo.EquipoId)
                    .Sum(p => p.GolesLocal ?? 0),
                JugadoresDestacados = equipo.Jugadores
                    .Select(j => new JugadorDestacadoViewModel
                    {
                        Jugador = j,
                        TotalGoles = j.Estadisticas.Sum(e => e.Goles),
                        TotalAsistencias = j.Estadisticas.Sum(e => e.Asistencias)
                    })
                    .OrderByDescending(j => j.TotalGoles)
                    .Take(5)
                    .ToList()
            };

            return View(viewModel);
        }
    }
}