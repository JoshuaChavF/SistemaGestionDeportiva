using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionDeportiva.Controllers
{
    [Authorize(Roles = "Administrador,Entrenador")]
    public class EstadisticasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EstadisticasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Estadisticas/Partido/5
        public async Task<IActionResult> Index(int partidoId)
        {
            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(p => p.PartidoId == partidoId);

            if (partido == null)
            {
                return NotFound();
            }

            var estadisticas = await _context.EstadisticasPartido
                .Include(e => e.Jugador)
                    .ThenInclude(j => j.Usuario)
                .Include(e => e.Jugador)
                    .ThenInclude(j => j.Equipo)
                .Where(e => e.PartidoId == partidoId)
                .ToListAsync();

            ViewData["Partido"] = partido;
            return View(estadisticas);
        }

        // GET: Estadisticas/Create/5
        public async Task<IActionResult> Create(int partidoId)
        {
            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(p => p.PartidoId == partidoId);

            if (partido == null)
            {
                return NotFound();
            }

            // Obtener jugadores de ambos equipos
            var jugadoresLocal = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoLocalId)
                .ToListAsync();

            var jugadoresVisitante = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoVisitanteId)
                .ToListAsync();

            var jugadores = jugadoresLocal.Concat(jugadoresVisitante).ToList();

            ViewData["JugadorId"] = new SelectList(jugadores, "JugadorId", "Usuario.NombreCompleto");
            ViewData["Partido"] = partido;

            return View();
        }

        // POST: Estadisticas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EstadisticaId,JugadorId,PartidoId,Goles,Asistencias,TarjetasAmarillas,TarjetasRojas,MinutosJugados,Notas")] EstadisticaPartido estadistica)
        {
            if (ModelState.IsValid)
            {
                _context.Add(estadistica);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { partidoId = estadistica.PartidoId });
            }

            // Si hay errores, recargar los datos necesarios para la vista
            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(p => p.PartidoId == estadistica.PartidoId);

            if (partido == null)
            {
                return NotFound();
            }

            var jugadoresLocal = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoLocalId)
                .ToListAsync();

            var jugadoresVisitante = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoVisitanteId)
                .ToListAsync();

            var jugadores = jugadoresLocal.Concat(jugadoresVisitante).ToList();

            ViewData["JugadorId"] = new SelectList(jugadores, "JugadorId", "Usuario.NombreCompleto", estadistica.JugadorId);
            ViewData["Partido"] = partido;

            return View(estadistica);
        }

        // GET: Estadisticas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var estadistica = await _context.EstadisticasPartido
                .Include(e => e.Jugador)
                    .ThenInclude(j => j.Usuario)
                .Include(e => e.Partido)
                .FirstOrDefaultAsync(e => e.EstadisticaId == id);

            if (estadistica == null)
            {
                return NotFound();
            }

            var partido = estadistica.Partido;
            var jugadoresLocal = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoLocalId)
                .ToListAsync();

            var jugadoresVisitante = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoVisitanteId)
                .ToListAsync();

            var jugadores = jugadoresLocal.Concat(jugadoresVisitante).ToList();

            ViewData["JugadorId"] = new SelectList(jugadores, "JugadorId", "Usuario.NombreCompleto", estadistica.JugadorId);
            ViewData["Partido"] = partido;

            return View(estadistica);
        }

        // POST: Estadisticas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EstadisticaId,JugadorId,PartidoId,Goles,Asistencias,TarjetasAmarillas,TarjetasRojas,MinutosJugados,Notas")] EstadisticaPartido estadistica)
        {
            if (id != estadistica.EstadisticaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(estadistica);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EstadisticaExists(estadistica.EstadisticaId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { partidoId = estadistica.PartidoId });
            }

            // Si hay errores, recargar los datos necesarios para la vista
            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(p => p.PartidoId == estadistica.PartidoId);

            if (partido == null)
            {
                return NotFound();
            }

            var jugadoresLocal = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoLocalId)
                .ToListAsync();

            var jugadoresVisitante = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoId == partido.EquipoVisitanteId)
                .ToListAsync();

            var jugadores = jugadoresLocal.Concat(jugadoresVisitante).ToList();

            ViewData["JugadorId"] = new SelectList(jugadores, "JugadorId", "Usuario.NombreCompleto", estadistica.JugadorId);
            ViewData["Partido"] = partido;

            return View(estadistica);
        }

        // GET: Estadisticas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var estadistica = await _context.EstadisticasPartido
                .Include(e => e.Jugador)
                    .ThenInclude(j => j.Usuario)
                .Include(e => e.Partido)
                .FirstOrDefaultAsync(e => e.EstadisticaId == id);

            if (estadistica == null)
            {
                return NotFound();
            }

            return View(estadistica);
        }

        // POST: Estadisticas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var estadistica = await _context.EstadisticasPartido.FindAsync(id);
            var partidoId = estadistica.PartidoId;

            _context.EstadisticasPartido.Remove(estadistica);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { partidoId });
        }

        // GET: Estadisticas/Jugador/5
        [AllowAnonymous]
        public async Task<IActionResult> Jugador(int jugadorId)
        {
            var jugador = await _context.Jugadores
                .Include(j => j.Usuario)
                .Include(j => j.Equipo)
                .FirstOrDefaultAsync(j => j.JugadorId == jugadorId);

            if (jugador == null)
            {
                return NotFound();
            }

            var estadisticas = await _context.EstadisticasPartido
                .Include(e => e.Partido)
                    .ThenInclude(p => p.EquipoLocal)
                .Include(e => e.Partido)
                    .ThenInclude(p => p.EquipoVisitante)
                .Where(e => e.JugadorId == jugadorId)
                .OrderByDescending(e => e.Partido.FechaHora)
                .ToListAsync();

            ViewData["Jugador"] = jugador;
            return View(estadisticas);
        }

        private bool EstadisticaExists(int id)
        {
            return _context.EstadisticasPartido.Any(e => e.EstadisticaId == id);
        }
    }
}