using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SistemaGestionDeportiva.Models.Partido;

namespace SistemaGestionDeportiva.Controllers
{
    [Authorize]
    public class PartidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartidosController> _logger;

        public PartidosController(ApplicationDbContext context, ILogger<PartidosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Partidos
        // GET: Partidos
        public async Task<IActionResult> Index(int? ligaId)
        {
            var ligasDisponibles = await _context.Ligas.ToListAsync();

            if (!ligasDisponibles.Any())
            {
                ViewBag.Mensaje = "No hay ligas disponibles";
                return View(new CalendarioViewModel { TodasLigas = ligasDisponibles });
            }

            ligaId ??= ligasDisponibles.First().LigaId;

            var ligaSeleccionada = await _context.Ligas
                .Include(l => l.Equipos)
                .FirstOrDefaultAsync(l => l.LigaId == ligaId);

            if (ligaSeleccionada == null) return NotFound();

            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Where(p => p.LigaId == ligaId)
                .OrderBy(p => p.FechaHora)
                .ToListAsync();

            var viewModel = new CalendarioViewModel
            {
                Liga = ligaSeleccionada,
                Partidos = partidos,
                TodasLigas = ligasDisponibles
            };
            var viewModels = new GoleadoresLigaViewModel();
            viewModels.Goleadores = await _context.Goles
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
            Console.WriteLine($"Goleadores encontrados: {viewModels.Goleadores.Count}");
            foreach (var g in viewModels.Goleadores)
            {
                Console.WriteLine($"{g.NombreJugador} - {g.Goles} goles");
            }

            ViewBag.LigaSeleccionadaId = ligaId;
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Calendario(int? ligaId, string fecha = null)
        {
            // Obtener todas las ligas disponibles para el dropdown
            var ligas = await _context.Ligas.ToListAsync();

            // Si no se especifica liga, usar la primera disponible
            if (!ligaId.HasValue && ligas.Any())
            {
                ligaId = ligas.First().LigaId;
            }

            var viewModel = new CalendarioViewModel
            {
                TodasLigas = ligas,
                ListaLigas = new SelectList(ligas, "LigaId", "Nombre", ligaId)
            };

            if (ligaId.HasValue)
            {
                var liga = await _context.Ligas
                    .Include(l => l.Partidos)
                        .ThenInclude(p => p.EquipoLocal)
                    .Include(l => l.Partidos)
                        .ThenInclude(p => p.EquipoVisitante)
                    .FirstOrDefaultAsync(l => l.LigaId == ligaId);

                if (liga == null) return NotFound();

                DateTime fechaFiltro;
                if (!DateTime.TryParse(fecha, out fechaFiltro))
                {
                    fechaFiltro = DateTime.Today;
                }

                // Filtrar partidos por fecha seleccionada
                var partidosFiltrados = liga.Partidos
                    .Where(p => p.FechaHora.Date == fechaFiltro.Date)
                    .OrderBy(p => p.FechaHora)
                    .ToList();

                viewModel.Liga = liga;
                viewModel.Partidos = partidosFiltrados;
                viewModel.FechaFiltro = fechaFiltro;
            }

            return View(viewModel);
        }



        // GET: Partidos/GetPartidosCalendar
        public async Task<IActionResult> GetPartidosCalendar()
        {
            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .ToListAsync();

            var eventos = partidos.Select(p => new
            {
                title = $"{p.EquipoLocal.Nombre} vs {p.EquipoVisitante.Nombre}",
                start = p.FechaHora.ToString("yyyy-MM-ddTHH:mm:ss"),
                url = Url.Action("Details", "Partidos", new { id = p.PartidoId }),
                color = p.Estado == "Finalizado" ? "#28a745" :
                       p.Estado == "EnCurso" ? "#ffc107" :
                       p.Estado == "Cancelado" ? "#dc3545" : "#007bff"
            });

            return Json(eventos);
        }

        // GET: Partidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partido = await _context.Partidos
                .Include(p => p.Liga)
                .Include(p => p.EquipoLocal)
                .ThenInclude(e => e.Jugadores)
                .Include(p => p.EquipoVisitante)
                .ThenInclude(e => e.Jugadores)
                .Include(p => p.Estadisticas)
                .ThenInclude(e => e.Jugador)
                .ThenInclude(j => j.Usuario)
                .Include(p => p.Goles)
                .ThenInclude(g => g.Jugador)
                .FirstOrDefaultAsync(m => m.PartidoId == id);

            if (partido == null)
            {
                return NotFound();
            }
            if (partido?.EquipoLocal?.Jugadores == null ||
    partido?.EquipoVisitante?.Jugadores == null)
            {
                // Manejar el caso donde faltan datos
                return NotFound("Datos del equipo o jugadores no encontrados");
            }

            ViewBag.JugadoresLocal = JsonConvert.SerializeObject(partido.EquipoLocal.Jugadores.Select(j => new {
                id = j.JugadorId,
                nombre = j.Nombre,
                posicion = j.Posicion
            }));

            ViewBag.JugadoresVisitante = JsonConvert.SerializeObject(partido.EquipoVisitante.Jugadores.Select(j => new {
                id = j.JugadorId,
                nombre = j.Nombre,
                posicion = j.Posicion
            }));

            return View(partido);
        }

        // GET: Partidos/Create
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Create()
        {
            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre");
            ViewData["EquipoLocalId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre");
            ViewData["EquipoVisitanteId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre");
            return View();
        }

        // POST: Partidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Create([Bind("PartidoId,LigaId,EquipoLocalId,EquipoVisitanteId,FechaHora,Ubicacion,Estado")] Partido partido)
        {
            if (ModelState.IsValid)
            {
                _context.Add(partido);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre", partido.LigaId);
            ViewData["EquipoLocalId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", partido.EquipoLocalId);
            ViewData["EquipoVisitanteId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", partido.EquipoVisitanteId);
            return View(partido);
        }

        // GET: Partidos/Edit/5
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null)
            {
                return NotFound();
            }

            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre", partido.LigaId);
            ViewData["EquipoLocalId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", partido.EquipoLocalId);
            ViewData["EquipoVisitanteId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", partido.EquipoVisitanteId);
            return View(partido);
        }

        // POST: Partidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Edit(int id, [Bind("PartidoId,LigaId,EquipoLocalId,EquipoVisitanteId,FechaHora,Ubicacion,Estado,GolesLocal,GolesVisitante")] Partido partido)
        {
            if (id != partido.PartidoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(partido);
                    await _context.SaveChangesAsync();

                    // Si el partido se marca como finalizado, actualizar estadísticas de la liga
                    if (partido.Estado == "Finalizado")
                    {
                        await ActualizarTablaPosiciones(partido.LigaId);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PartidoExists(partido.PartidoId))
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

            ViewData["LigaId"] = new SelectList(await _context.Ligas.ToListAsync(), "LigaId", "Nombre", partido.LigaId);
            ViewData["EquipoLocalId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", partido.EquipoLocalId);
            ViewData["EquipoVisitanteId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", partido.EquipoVisitanteId);
            return View(partido);
        }

        // GET: Partidos/Delete/5
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partido = await _context.Partidos
                .Include(p => p.Liga)
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(m => m.PartidoId == id);

            if (partido == null)
            {
                return NotFound();
            }

            return View(partido);
        }

        // POST: Partidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            _context.Partidos.Remove(partido);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Partidos/RegistrarResultado/5
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> RegistrarResultado(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(m => m.PartidoId == id);

            if (partido == null)
            {
                return NotFound();
            }

            var viewModel = new RegistrarResultadoViewModel
            {
                PartidoId = partido.PartidoId,
                EquipoLocalNombre = partido.EquipoLocal.Nombre,
                EquipoVisitanteNombre = partido.EquipoVisitante.Nombre,
                GolesLocal = partido.GolesLocal ?? 0,
                GolesVisitante = partido.GolesVisitante ?? 0
            };

            return View(viewModel);
        }

        // POST: Partidos/RegistrarResultado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> RegistrarResultado(int id, RegistrarResultadoViewModel model)
        {
            if (id != model.PartidoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var partido = await _context.Partidos.FindAsync(id);
                if (partido == null)
                {
                    return NotFound();
                }

                partido.GolesLocal = model.GolesLocal;
                partido.GolesVisitante = model.GolesVisitante;
                partido.Estado = "Finalizado";

                _context.Update(partido);
                await _context.SaveChangesAsync();

                // Actualizar tabla de posiciones
                await ActualizarTablaPosiciones(partido.LigaId);

                return RedirectToAction(nameof(Details), new { id });
            }

            // Si llegamos aquí, algo falló, recargar datos necesarios para la vista
            var partidoReload = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(m => m.PartidoId == id);

            if (partidoReload == null)
            {
                return NotFound();
            }

            model.EquipoLocalNombre = partidoReload.EquipoLocal.Nombre;
            model.EquipoVisitanteNombre = partidoReload.EquipoVisitante.Nombre;

            return View(model);
        }

        private async Task ActualizarTablaPosiciones(int ligaId)
        {
            var liga = await _context.Ligas
                .Include(l => l.Equipos)
                .Include(l => l.Partidos)
                    .ThenInclude(p => p.EquipoLocal)
                .Include(l => l.Partidos)
                    .ThenInclude(p => p.EquipoVisitante)
                .FirstOrDefaultAsync(l => l.LigaId == ligaId);

            if (liga != null)
            {
                foreach (var equipo in liga.Equipos)
                {
                    // Reiniciar estadísticas
                    equipo.PartidosJugados = 0;
                    equipo.PartidosGanados = 0;
                    equipo.PartidosEmpatados = 0;
                    equipo.PartidosPerdidos = 0;
                    equipo.GolesAFavor = 0;
                    equipo.GolesEnContra = 0;

                    // Calcular estadísticas basadas en partidos finalizados
                    var partidosLocal = liga.Partidos
                        .Where(p => p.EquipoLocalId == equipo.EquipoId && p.Estado == "Finalizado")
                        .ToList();

                    var partidosVisitante = liga.Partidos
                        .Where(p => p.EquipoVisitanteId == equipo.EquipoId && p.Estado == "Finalizado")
                        .ToList();

                    equipo.PartidosJugados = partidosLocal.Count + partidosVisitante.Count;

                    foreach (var partido in partidosLocal)
                    {
                        equipo.GolesAFavor += partido.GolesLocal ?? 0;
                        equipo.GolesEnContra += partido.GolesVisitante ?? 0;

                        if (partido.GolesLocal > partido.GolesVisitante)
                            equipo.PartidosGanados++;
                        else if (partido.GolesLocal == partido.GolesVisitante)
                            equipo.PartidosEmpatados++;
                        else
                            equipo.PartidosPerdidos++;
                    }

                    foreach (var partido in partidosVisitante)
                    {
                        equipo.GolesAFavor += partido.GolesVisitante ?? 0;
                        equipo.GolesEnContra += partido.GolesLocal ?? 0;

                        if (partido.GolesVisitante > partido.GolesLocal)
                            equipo.PartidosGanados++;
                        else if (partido.GolesVisitante == partido.GolesLocal)
                            equipo.PartidosEmpatados++;
                        else
                            equipo.PartidosPerdidos++;
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        private bool PartidoExists(int id)
        {
            return _context.Partidos.Any(e => e.PartidoId == id);
        }
        [HttpPost]
        [Authorize(Roles = "Administrador,Arbitro")]
        public async Task<IActionResult> ActualizarPartido(int id, [FromForm] PartidoViewModel model)
        {
            var partido = await _context.Partidos.FindAsync(id);

            if (partido == null) return NotFound();

            partido.GolesLocal = model.GolesLocal;
            partido.GolesVisitante = model.GolesVisitante;
            partido.TarjetasAmarillasLocal = model.TarjetasAmarillasLocal;
            partido.TarjetasRojasLocal = model.TarjetasRojasLocal;
            partido.TarjetasAmarillasVisitante = model.TarjetasAmarillasVisitante;
            partido.TarjetasRojasVisitante = model.TarjetasRojasVisitante;
            partido.TirosAlArcoLocal = model.TirosAlArcoLocal;
            partido.TirosAlArcoVisitante = model.TirosAlArcoVisitante;
            partido.PosesionLocal = model.PosesionLocal;
            partido.PosesionVisitante = 100 - model.PosesionLocal;
            partido.Observaciones = model.Observaciones;

            if (model.Finalizar)
            {
                partido.Estado = "Finalizado";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("DetallePartido", new { id });
        }
       
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GenerarFixture(int ligaId, DateTime fechaInicio, DateTime fechaFin)
        {
            var liga = await _context.Ligas
                .Include(l => l.Equipos)
                .FirstOrDefaultAsync(l => l.LigaId == ligaId);

            if (liga == null || liga.Equipos.Count < 2)
            {
                return NotFound("La liga no existe o no tiene suficientes equipos");
            }

            var equipos = liga.Equipos.ToList();
            var rnd = new Random();
            var partidos = new List<Partido>();
            var fechaActual = fechaInicio;

            // Generar partidos de ida
            for (int i = 0; i < equipos.Count; i++)
            {
                for (int j = i + 1; j < equipos.Count; j++)
                {
                    partidos.Add(new Partido
                    {
                        LigaId = ligaId,
                        EquipoLocalId = equipos[i].EquipoId,
                        EquipoVisitanteId = equipos[j].EquipoId,
                        FechaHora = fechaActual.AddDays(rnd.Next(0, (fechaFin - fechaInicio).Days / 2)),
                        Estado = "Programado"
                    });
                }
            }

            // Generar partidos de vuelta
            for (int i = 0; i < equipos.Count; i++)
            {
                for (int j = i + 1; j < equipos.Count; j++)
                {
                    partidos.Add(new Partido
                    {
                        LigaId = ligaId,
                        EquipoLocalId = equipos[j].EquipoId,
                        EquipoVisitanteId = equipos[i].EquipoId,
                        FechaHora = fechaActual.AddDays((fechaFin - fechaInicio).Days / 2 + rnd.Next(0, (fechaFin - fechaInicio).Days / 2)),
                        Estado = "Programado"
                    });
                }
            }

            _context.Partidos.AddRange(partidos);
            await _context.SaveChangesAsync();

            return RedirectToAction("Calendario", new { ligaId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarPartidos(int ligaId)
        {
            try
            {
                // Verificar si la liga existe
                var liga = await _context.Ligas
                    .Include(l => l.Equipos)
                    .FirstOrDefaultAsync(l => l.LigaId == ligaId);

                if (liga == null)
                {
                    TempData["ErrorMessage"] = "La liga no existe";
                    return RedirectToAction("Index", new { ligaId });
                }

                // Verificar que haya al menos 2 equipos
                if (liga.Equipos.Count < 2)
                {
                    TempData["ErrorMessage"] = "La liga debe tener al menos 2 equipos";
                    return RedirectToAction("Index", new { ligaId });
                }

                // Verificar si ya hay partidos
                var existenPartidos = await _context.Partidos.AnyAsync(p => p.LigaId == ligaId);
                if (existenPartidos)
                {
                    TempData["WarningMessage"] = "Ya existen partidos generados para esta liga";
                    TempData["LigaIdParaRegenerar"] = ligaId;
                    return RedirectToAction("Index", new { ligaId });
                }

                // Generar los partidos
                var partidosGenerados = await GenerarPartidosParaLiga(liga);

                // Guardar en la base de datos
                await GuardarPartidosEnLotes(partidosGenerados);

                TempData["SuccessMessage"] = $"Se generaron {partidosGenerados.Count} partidos exitosamente";
                return RedirectToAction("Index", new { ligaId });
            }
            catch (DbUpdateException dbEx)
            {
                // Capturar error específico de base de datos
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger?.LogError(dbEx, "Error de base de datos al generar partidos");
                TempData["ErrorMessage"] = $"Error al guardar partidos: {innerMessage}";
                return RedirectToAction("Index", new { ligaId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al generar partidos");
                TempData["ErrorMessage"] = $"Error inesperado: {ex.Message}";
                return RedirectToAction("Index", new { ligaId });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerarPartidos(int ligaId)
        {
            try
            {
                // Eliminar partidos existentes
                var partidosExistentes = _context.Partidos.Where(p => p.LigaId == ligaId);
                _context.Partidos.RemoveRange(partidosExistentes);
                await _context.SaveChangesAsync();

                // Generar nuevos partidos
                return await GenerarPartidos(ligaId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al regenerar partidos");
                TempData["ErrorMessage"] = $"Error al regenerar partidos: {ex.Message}";
                return RedirectToAction("Index", new { ligaId });
            }
        }
        private async Task GuardarPartidosEnLotes(List<Partido> partidos)
        {
            const int tamañoLote = 10; // Guardar de 10 en 10 para evitar timeout

            for (int i = 0; i < partidos.Count; i += tamañoLote)
            {
                var lote = partidos.Skip(i).Take(tamañoLote).ToList();
                _context.Partidos.AddRange(lote);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<List<Partido>> GenerarCalendarioPartidos(Liga liga)
        {
            var partidos = new List<Partido>();
            var equipos = liga.Equipos.ToList();
            var rnd = new Random();
            var diasTemporada = (liga.FechaFin - liga.FechaInicio).Days;

            // Generar partidos de ida y vuelta
            for (int i = 0; i < equipos.Count; i++)
            {
                for (int j = i + 1; j < equipos.Count; j++)
                {
                    var equipoLocal = equipos[i];
                    var equipoVisitante = equipos[j];

                    // Fechas aleatorias pero dentro del rango de la liga
                    var fechaIda = liga.FechaInicio.AddDays(rnd.Next(0, diasTemporada / 2));
                    var fechaVuelta = liga.FechaInicio.AddDays((diasTemporada / 2) + rnd.Next(0, diasTemporada / 2));

                    // Asegurar que la vuelta sea después de la ida
                    if (fechaVuelta <= fechaIda)
                    {
                        fechaVuelta = fechaIda.AddDays(1);
                    }

                    // Partido de ida
                    partidos.Add(new Partido
                    {
                        LigaId = liga.LigaId,
                        EquipoLocalId = equipoLocal.EquipoId,
                        EquipoVisitanteId = equipoVisitante.EquipoId,
                        FechaHora = fechaIda,
                        Estado = "Programado",
                        GolesLocal = 0,
                        GolesVisitante = 0,
                        TarjetasAmarillasLocal = 0,
                        TarjetasAmarillasVisitante = 0,
                        TarjetasRojasLocal = 0,
                        TarjetasRojasVisitante = 0,
                        TirosAlArcoLocal = 0,
                        TirosAlArcoVisitante = 0,
                        PosesionLocal = 50,
                        PosesionVisitante = 50,
                        Observaciones = "Partido generado automáticamente"
                    });

                    // Partido de vuelta
                    partidos.Add(new Partido
                    {
                        LigaId = liga.LigaId,
                        EquipoLocalId = equipoVisitante.EquipoId,
                        EquipoVisitanteId = equipoLocal.EquipoId,
                        FechaHora = fechaVuelta,
                        Estado = "Programado",
                        GolesLocal = 0,
                        GolesVisitante = 0,
                        TarjetasAmarillasLocal = 0,
                        TarjetasAmarillasVisitante = 0,
                        TarjetasRojasLocal = 0,
                        TarjetasRojasVisitante = 0,
                        TirosAlArcoLocal = 0,
                        TirosAlArcoVisitante = 0,
                        PosesionLocal = 50,
                        PosesionVisitante = 50,
                        Observaciones = "Partido generado automáticamente"
                    });
                }
            }

            return partidos;
        }

        // Métodos auxiliares privados para mejor organización
        private async Task<List<Partido>> GenerarPartidosParaLiga(Liga liga)
        {
            var partidos = new List<Partido>();
            var equipos = liga.Equipos.ToList();
            var rnd = new Random();
            var diasTemporada = (liga.FechaFin - liga.FechaInicio).Days;

            // Generar partidos de ida y vuelta
            for (int i = 0; i < equipos.Count; i++)
            {
                for (int j = i + 1; j < equipos.Count; j++)
                {
                    var equipoLocal = equipos[i];
                    var equipoVisitante = equipos[j];

                    // Validar que no sea el mismo equipo
                    if (equipoLocal.EquipoId == equipoVisitante.EquipoId)
                        continue;

                    // Generar fechas válidas
                    var fechaIda = liga.FechaInicio.AddDays(rnd.Next(0, diasTemporada / 2));
                    var fechaVuelta = liga.FechaInicio.AddDays((diasTemporada / 2) + rnd.Next(0, diasTemporada / 2));

                    // Asegurar que la vuelta sea después de la ida
                    if (fechaVuelta <= fechaIda)
                    {
                        fechaVuelta = fechaIda.AddDays(1);
                    }

                    // Partido de ida
                    partidos.Add(CrearNuevoPartido(
                        liga.LigaId,
                        equipoLocal.EquipoId,
                        equipoVisitante.EquipoId,
                        fechaIda
                    ));

                    // Partido de vuelta
                    partidos.Add(CrearNuevoPartido(
                        liga.LigaId,
                        equipoVisitante.EquipoId,
                        equipoLocal.EquipoId,
                        fechaVuelta
                    ));
                }
            }

            return partidos;
        }
        private Partido CrearNuevoPartido(int ligaId, int equipoLocalId, int equipoVisitanteId, DateTime fecha)
        {
            return new Partido
            {
                LigaId = ligaId,
                EquipoLocalId = equipoLocalId,
                EquipoVisitanteId = equipoVisitanteId,
                FechaHora = fecha,
                Estado = "Programado",
                GolesLocal = 0,
                GolesVisitante = 0,
                TarjetasAmarillasLocal = 0,
                TarjetasAmarillasVisitante = 0,
                TarjetasRojasLocal = 0,
                TarjetasRojasVisitante = 0,
                TirosAlArcoLocal = 0,
                TirosAlArcoVisitante = 0,
                PosesionLocal = 50,
                PosesionVisitante = 50,
                Observaciones = "Partido generado automáticamente"
            };
        }


        private (DateTime fechaIda, DateTime fechaVuelta) GenerarFechasPartidos(Liga liga, int diasDisponibles, Random rnd)
        {
            var fechaIda = liga.FechaInicio.AddDays(rnd.Next(0, diasDisponibles / 2));
            var fechaVuelta = liga.FechaInicio.AddDays(diasDisponibles / 2 + rnd.Next(0, diasDisponibles / 2));

            // Asegurar que haya al menos un día entre partidos
            if (fechaVuelta <= fechaIda)
            {
                fechaVuelta = fechaIda.AddDays(1);
            }

            return (fechaIda, fechaVuelta);
        }

        private Partido CrearPartido(int ligaId, int equipoLocalId, int equipoVisitanteId, DateTime fecha)
        {
            return new Partido
            {
                LigaId = ligaId,
                EquipoLocalId = equipoLocalId,
                EquipoVisitanteId = equipoVisitanteId,
                FechaHora = fecha,
                Estado = "Programado",
                GolesLocal = 0,
                GolesVisitante = 0,
                TarjetasAmarillasLocal = 0,
                TarjetasAmarillasVisitante = 0,
                TarjetasRojasLocal = 0,
                TarjetasRojasVisitante = 0,
                TirosAlArcoLocal = 0,
                TirosAlArcoVisitante = 0,
                PosesionLocal = 50,
                Observaciones = string.Empty
            };
        }

        private async Task EliminarPartidosExistentes(int ligaId)
        {
            var partidosExistentes = _context.Partidos.Where(p => p.LigaId == ligaId);
            _context.Partidos.RemoveRange(partidosExistentes);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> SimularPartidosDeHoy(int ligaId)
        {
            var partidosDeHoy = await _context.Partidos
        .Include(p => p.EquipoLocal)
            .ThenInclude(e => e.Jugadores)
        .Include(p => p.EquipoVisitante)
            .ThenInclude(e => e.Jugadores)
        .Where(p => p.LigaId == ligaId &&
                    p.FechaHora.Date == DateTime.Today.Date &&
                    p.Estado == "Programado")
        .ToListAsync();

            var rnd = new Random();

            foreach (var partido in partidosDeHoy)
            {
                partido.GolesLocal = rnd.Next(0, 5);
                partido.GolesVisitante = rnd.Next(0, 5);
                partido.TarjetasAmarillasLocal = rnd.Next(0, 3);
                partido.TarjetasAmarillasVisitante = rnd.Next(0, 3);
                partido.TarjetasRojasLocal = rnd.Next(0, 2);
                partido.TarjetasRojasVisitante = rnd.Next(0, 2);
                partido.TirosAlArcoLocal = rnd.Next(5, 20);
                partido.TirosAlArcoVisitante = rnd.Next(5, 20);
                partido.PosesionLocal = rnd.Next(30, 70);
                partido.Estado = "Finalizado";
                // Eliminar goles existentes (si los hay)
                var golesExistentes = _context.Goles.Where(g => g.PartidoId == partido.PartidoId);
                _context.Goles.RemoveRange(golesExistentes);

                // Registrar goles locales
                for (int i = 0; i < partido.GolesLocal; i++)
                {
                    var jugador = partido.EquipoLocal.Jugadores.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    if (jugador != null)
                    {
                        _context.Goles.Add(new Gol
                        {
                            PartidoId = partido.PartidoId,
                            JugadorId = jugador.JugadorId,
                            EquipoId = partido.EquipoLocalId,
                            Minuto = rnd.Next(1, 90),
                            EsAutogol = false,
                            EsPenal = rnd.Next(0, 10) > 8 // 20% de probabilidad de ser penal
                        });
                    }
                }

                // Registrar goles visitantes
                for (int i = 0; i < partido.GolesVisitante; i++)
                {
                    var jugador = partido.EquipoVisitante.Jugadores.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    if (jugador != null)
                    {
                        _context.Goles.Add(new Gol
                        {
                            PartidoId = partido.PartidoId,
                            JugadorId = jugador.JugadorId,
                            EquipoId = partido.EquipoVisitanteId,
                            Minuto = rnd.Next(1, 90),
                            EsAutogol = false,
                            EsPenal = rnd.Next(0, 10) > 8 // 20% de probabilidad de ser penal
                        });
                    }
                }
                // Eliminar estadísticas existentes (si las hay)
                var estadisticasExistentes = _context.EstadisticasJugador.Where(e => e.PartidoId == partido.PartidoId);
                _context.EstadisticasJugador.RemoveRange(estadisticasExistentes);

                // Registrar tarjetas amarillas locales
                for (int i = 0; i < partido.TarjetasAmarillasLocal; i++)
                {
                    var jugador = partido.EquipoLocal.Jugadores.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    if (jugador != null)
                    {
                        _context.EstadisticasJugador.Add(new EstadisticaJugador
                        {
                            PartidoId = partido.PartidoId,
                            JugadorId = jugador.JugadorId,
                            EquipoId = partido.EquipoLocalId,
                            TarjetaAmarilla = true,
                            TarjetaRoja = false,
                            Minuto = rnd.Next(1, 90)
                        });
                    }
                }

                // Registrar tarjetas amarillas visitantes
                for (int i = 0; i < partido.TarjetasAmarillasVisitante; i++)
                {
                    var jugador = partido.EquipoVisitante.Jugadores.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    if (jugador != null)
                    {
                        _context.EstadisticasJugador.Add(new EstadisticaJugador
                        {
                            PartidoId = partido.PartidoId,
                            JugadorId = jugador.JugadorId,
                            EquipoId = partido.EquipoVisitanteId,
                            TarjetaAmarilla = true,
                            TarjetaRoja = false,
                            Minuto = rnd.Next(1, 90)
                        });
                    }
                }

                // Registrar tarjetas rojas locales
                for (int i = 0; i < partido.TarjetasRojasLocal; i++)
                {
                    var jugador = partido.EquipoLocal.Jugadores.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    if (jugador != null)
                    {
                        _context.EstadisticasJugador.Add(new EstadisticaJugador
                        {
                            PartidoId = partido.PartidoId,
                            JugadorId = jugador.JugadorId,
                            EquipoId = partido.EquipoLocalId,
                            TarjetaAmarilla = false,
                            TarjetaRoja = true,
                            Minuto = rnd.Next(1, 90)
                        });
                    }
                }

                // Registrar tarjetas rojas visitantes
                for (int i = 0; i < partido.TarjetasRojasVisitante; i++)
                {
                    var jugador = partido.EquipoVisitante.Jugadores.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    if (jugador != null)
                    {
                        _context.EstadisticasJugador.Add(new EstadisticaJugador
                        {
                            PartidoId = partido.PartidoId,
                            JugadorId = jugador.JugadorId,
                            EquipoId = partido.EquipoVisitanteId,
                            TarjetaAmarilla = false,
                            TarjetaRoja = true,
                            Minuto = rnd.Next(1, 90)
                        });
                    }
                }

                // Generar observaciones del partido
                var observaciones = new StringBuilder();
                observaciones.AppendLine($"Partido simulado automáticamente el {DateTime.Now:dd/MM/yyyy}");
                observaciones.AppendLine($"Resultado final: {partido.EquipoLocal.Nombre} {partido.GolesLocal} - {partido.GolesVisitante} {partido.EquipoVisitante.Nombre}");

                if (partido.GolesLocal > 0 || partido.GolesVisitante > 0)
                {
                    observaciones.AppendLine("Goleadores:");
                    var goleadores = await _context.Goles
                        .Include(g => g.Jugador)
                        .Where(g => g.PartidoId == partido.PartidoId)
                        .ToListAsync();

                    foreach (var gol in goleadores)
                    {
                        observaciones.AppendLine($"- {gol.Jugador.Nombre} {(gol.EsPenal ? "(Penal)" : $"(Min. {gol.Minuto})")}");
                    }
                }

                if (partido.TarjetasAmarillasLocal > 0 || partido.TarjetasAmarillasVisitante > 0 ||
                    partido.TarjetasRojasLocal > 0 || partido.TarjetasRojasVisitante > 0)
                {
                    observaciones.AppendLine("Tarjetas:");
                    var tarjetas = await _context.EstadisticasJugador
                        .Include(e => e.Jugador)
                        .Where(e => e.PartidoId == partido.PartidoId)
                        .ToListAsync();

                    foreach (var tarjeta in tarjetas)
                    {
                        observaciones.AppendLine($"- {tarjeta.Jugador.Nombre}: {(tarjeta.TarjetaRoja ? "Roja" : "Amarilla")} (Min. {tarjeta.Minuto})");
                    }
                }

                partido.Observaciones = observaciones.ToString();
            }

        await _context.SaveChangesAsync();
            // Actualizar tabla de posiciones
            await ActualizarTablaPosiciones(ligaId);

            return Ok(new
            {
                success = true,
                message = $"Se simularon {partidosDeHoy.Count} partidos correctamente",
                partidosSimulados = partidosDeHoy.Select(p => new {
                    id = p.PartidoId,
                    local = p.EquipoLocal.Nombre,
                    visitante = p.EquipoVisitante.Nombre,
                    resultado = $"{p.GolesLocal} - {p.GolesVisitante}"
                })
            });
        }

        /*[HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> SimularPartido(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null)
            {
                return NotFound();
            }

            var rnd = new Random();
            partido.GolesLocal = rnd.Next(0, 5);
            partido.GolesVisitante = rnd.Next(0, 5);
            partido.TarjetasAmarillasLocal = rnd.Next(0, 3);
            partido.TarjetasAmarillasVisitante = rnd.Next(0, 3);
            partido.TarjetasRojasLocal = rnd.Next(0, 2);
            partido.TarjetasRojasVisitante = rnd.Next(0, 2);
            partido.TirosAlArcoLocal = rnd.Next(5, 20);
            partido.TirosAlArcoVisitante = rnd.Next(5, 20);
            partido.PosesionLocal = rnd.Next(30, 70);
            partido.Estado = "Finalizado";

            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> Detalles(int id)
        {
            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .FirstOrDefaultAsync(p => p.PartidoId == id);

            if (partido == null)
            {
                return NotFound();
            }

            return PartialView("_DetallesPartido", partido);
        }*/

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GenerarPartidosCompletos(int ligaId)
        {
            try
            {
                // Eliminar partidos existentes si los hay
                var partidosExistentes = _context.Partidos.Where(p => p.LigaId == ligaId);
                _context.Partidos.RemoveRange(partidosExistentes);
                await _context.SaveChangesAsync();

                // Generar nuevos partidos con datos completos
                var liga = await _context.Ligas
                    .Include(l => l.Equipos)
                    .FirstOrDefaultAsync(l => l.LigaId == ligaId);

                if (liga == null || liga.Equipos.Count < 2)
                {
                    TempData["ErrorMessage"] = "La liga no existe o no tiene suficientes equipos";
                    return RedirectToAction("Index", new { ligaId });
                }

                var partidosGenerados = new List<Partido>();
                var equipos = liga.Equipos.ToList();
                var rnd = new Random();
                var fechaActual = liga.FechaInicio;

                // Generar partidos de ida y vuelta
                for (int i = 0; i < equipos.Count; i++)
                {
                    for (int j = i + 1; j < equipos.Count; j++)
                    {
                        // Partido de ida
                        var partidoIda = CrearPartidoCompleto(ligaId, equipos[i].EquipoId, equipos[j].EquipoId, fechaActual, rnd);
                        partidosGenerados.Add(partidoIda);

                        // Partido de vuelta (2 semanas después)
                        var partidoVuelta = CrearPartidoCompleto(ligaId, equipos[j].EquipoId, equipos[i].EquipoId, fechaActual.AddDays(14), rnd);
                        partidosGenerados.Add(partidoVuelta);

                        fechaActual = fechaActual.AddDays(7); // Siguiente fecha en una semana
                    }
                }

                _context.Partidos.AddRange(partidosGenerados);
                await _context.SaveChangesAsync();

                // Actualizar tabla de posiciones
                await ActualizarTablaPosiciones(ligaId);

                TempData["SuccessMessage"] = $"Se generaron {partidosGenerados.Count} partidos completos y se actualizó la tabla de posiciones";
                return RedirectToAction("Index", new { ligaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar partidos completos");
                TempData["ErrorMessage"] = $"Error al generar partidos: {ex.Message}";
                return RedirectToAction("Index", new { ligaId });
            }
        }

        private Partido CrearPartidoCompleto(int ligaId, int equipoLocalId, int equipoVisitanteId, DateTime fecha, Random rnd)
        {
            // Generar datos aleatorios realistas
            var golesLocal = rnd.Next(0, 5);
            var golesVisitante = rnd.Next(0, 5);
            var estado = fecha < DateTime.Now ? "Finalizado" : "Programado";

            return new Partido
            {
                LigaId = ligaId,
                EquipoLocalId = equipoLocalId,
                EquipoVisitanteId = equipoVisitanteId,
                FechaHora = fecha,
                Estado = estado,
                GolesLocal = estado == "Finalizado" ? golesLocal : null,
                GolesVisitante = estado == "Finalizado" ? golesVisitante : null,
                TarjetasAmarillasLocal = rnd.Next(0, 5),
                TarjetasRojasLocal = rnd.Next(0, 2),
                TarjetasAmarillasVisitante = rnd.Next(0, 5),
                TarjetasRojasVisitante = rnd.Next(0, 2),
                TirosAlArcoLocal = rnd.Next(5, 25),
                TirosAlArcoVisitante = rnd.Next(5, 25),
                PosesionLocal = rnd.Next(35, 65),
                Ubicacion = "Estadio " + (new[] { "Principal", "Municipal", "De la Ciudad", "Nacional" }[rnd.Next(0, 4)]),
                Observaciones = estado == "Finalizado" ? "Partido simulado automáticamente" : "Partido programado"
            };
        }
        [HttpGet]
        public async Task<IActionResult> TablaPosiciones(int ligaId)
        {
            var liga = await _context.Ligas.FindAsync(ligaId);
            if (liga == null)
            {
                return NotFound();
            }

            var equipos = await _context.Equipos
                .Where(e => e.LigaId == ligaId)
                .Include(e => e.PartidosLocal)
                .Include(e => e.PartidosVisitante)
                .ToListAsync();

            var tabla = equipos.Select(e => {
                var partidosLocal = e.PartidosLocal.Where(p => p.Estado == "Finalizado").ToList();
                var partidosVisitante = e.PartidosVisitante.Where(p => p.Estado == "Finalizado").ToList();

                // Calcular estadísticas
                var pj = partidosLocal.Count + partidosVisitante.Count;
                var pg = partidosLocal.Count(p => p.GolesLocal > p.GolesVisitante) +
                         partidosVisitante.Count(p => p.GolesVisitante > p.GolesLocal);
                var pe = partidosLocal.Count(p => p.GolesLocal == p.GolesVisitante) +
                         partidosVisitante.Count(p => p.GolesVisitante == p.GolesLocal);
                var pp = pj - pg - pe;
                var gf = partidosLocal.Sum(p => p.GolesLocal ?? 0) +
                         partidosVisitante.Sum(p => p.GolesVisitante ?? 0);
                var gc = partidosLocal.Sum(p => p.GolesVisitante ?? 0) +
                         partidosVisitante.Sum(p => p.GolesLocal ?? 0);
                var dg = gf - gc;
                var puntos = pg * 3 + pe;

                // Obtener últimos resultados (para la forma)
                var ultimosResultados = partidosLocal
                    .Concat(partidosVisitante)
                    .OrderByDescending(p => p.FechaHora)
                    .Take(5)
                    .Select(p => p.EquipoLocalId == e.EquipoId
                        ? (p.GolesLocal > p.GolesVisitante ? 'W' : p.GolesLocal == p.GolesVisitante ? 'D' : 'L')
                        : (p.GolesVisitante > p.GolesLocal ? 'W' : p.GolesVisitante == p.GolesLocal ? 'D' : 'L'))
                    .ToList();

                return new PosicionViewModel
                {
                    Equipo = e,
                    PJ = pj,
                    PG = pg,
                    PE = pe,
                    PP = pp,
                    GF = gf,
                    GC = gc,
                    DG = dg,
                    Puntos = puntos,
                    UltimosResultados = ultimosResultados
                };
            })
            .OrderByDescending(p => p.Puntos)
            .ThenByDescending(p => p.DG)
            .ThenByDescending(p => p.GF)
            .ToList();

            ViewBag.LigaId = ligaId;
            return View(tabla);
        }
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> SimularPartido(
    int id, [FromForm] PartidoSimulacionViewModel model)
        {
            var partido = await _context.Partidos
       .Include(p => p.EquipoLocal)
           .ThenInclude(e => e.Jugadores)
       .Include(p => p.EquipoVisitante)
           .ThenInclude(e => e.Jugadores)
       .Include(p => p.Goles)
       .FirstOrDefaultAsync(p => p.PartidoId == id);
            // var partido = await _context.Partidos.FindAsync(id);
            if (partido == null)
            {
                return NotFound();
            }
            // Validar datos mínimos
            if (model.GolesLocal < 0 || model.GolesVisitante < 0)
            {
                ModelState.AddModelError("", "Los goles no pueden ser negativos");
                return View("Details", partido);
            }
            // Actualizar datos del partido
            partido.GolesLocal = model.GolesLocal;
            partido.GolesVisitante = model.GolesVisitante;
            partido.TarjetasAmarillasLocal = model.TarjetasAmarillasLocal;
            partido.TarjetasAmarillasVisitante = model.TarjetasAmarillasVisitante;
            partido.TarjetasRojasLocal = model.TarjetasRojasLocal;
            partido.TarjetasRojasVisitante = model.TarjetasRojasVisitante;
            partido.TirosAlArcoLocal = model.TirosAlArcoLocal;
            partido.TirosAlArcoVisitante = model.TirosAlArcoVisitante;
            partido.PosesionLocal = model.PosesionLocal;
            partido.FaltasLocal = model.FaltasLocal;
            partido.FaltasVisitante = model.FaltasVisitante;
            partido.TiempoExtra = model.TiempoExtra;
            partido.Penales = model.Penales;
            partido.Estado = "Finalizado";

            // Construir observaciones detalladas
            var obsBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(model.Observaciones))
            {
                obsBuilder.AppendLine(model.Observaciones);
            }
            if (model.TiempoExtra)
            {
                obsBuilder.AppendLine("El partido tuvo tiempo extra.");
            }
            if (model.Penales)
            {
                obsBuilder.AppendLine($"Definición por penales: {(model.GolesLocal > model.GolesVisitante ? partido.EquipoLocal.Nombre : partido.EquipoVisitante.Nombre)} ganó.");
            }

            partido.Observaciones = obsBuilder.ToString();

            // Solo procesar goleadores si el partido tiene goles
            if (model.GolesLocal > 0 || model.GolesVisitante > 0)
            {
                // Eliminar goles existentes
                var golesExistentes = _context.Goles.Where(g => g.PartidoId == id);
                _context.Goles.RemoveRange(golesExistentes);

                // Procesar goleadores locales
                if (model.GoleadoresLocal != null)
                {
                    foreach (var jugadorId in model.GoleadoresLocal)
                    {
                        if (jugadorId > 0) // Validar ID válido
                        {
                            _context.Goles.Add(new Gol
                            {
                                PartidoId = id,
                                JugadorId = jugadorId,
                                EquipoId = partido.EquipoLocalId,
                                Minuto = 0,
                                EsAutogol = false
                            });
                        }
                    }
                }

                // Procesar goleadores visitantes
                if (model.GoleadoresVisitante != null)
                {
                    foreach (var jugadorId in model.GoleadoresVisitante)
                    {
                        if (jugadorId > 0) // Validar ID válido
                        {
                            _context.Goles.Add(new Gol
                            {
                                PartidoId = id,
                                JugadorId = jugadorId,
                                EquipoId = partido.EquipoVisitanteId,
                                Minuto = 0,
                                EsAutogol = false
                            });
                        }
                    }
                }
            }

            _context.Update(partido);
            await _context.SaveChangesAsync();

            // Actualizar tabla de posiciones
            await ActualizarTablaPosiciones(partido.LigaId);

            TempData["SuccessMessage"] = "Resultado del partido guardado exitosamente";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarFecha(int id, DateTime nuevaFecha)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null)
            {
                return NotFound();
            }

            partido.FechaHora = nuevaFecha;
            partido.FechaModificacion = DateTime.Now;
            partido.Estado = "Programado"; 
            _logger.LogInformation($"Fecha actualizada. Nueva fecha: {nuevaFecha}, Estado: {partido.Estado}");
            _context.Update(partido);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Fecha del partido actualizada correctamente";
            return RedirectToAction("Details", new { id });
        }

        public List<Goleador> ExtraerGoleadoresDesdeObservaciones(string observaciones, int partidoId, int ligaId)
        {
            var goleadores = new List<Goleador>();

            if (string.IsNullOrEmpty(observaciones))
                return goleadores;

            var regexAmarillas = new Regex(@"🟨 (\d+)' - Tarjeta amarilla para (.+?) \(");
            var regexRojas = new Regex(@"🟥 (\d+)' - Tarjeta roja para (.+?) \(");
            // Buscar la sección de GOLEADORES
            var startIndex = observaciones.IndexOf("GOLEADORES:");
            if (startIndex == -1) return goleadores;

            var goleadoresSection = observaciones.Substring(startIndex);
            var lines = goleadoresSection.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains("•") && line.Contains("-"))
                {
                    var parts = line.Split(new[] { "•", "-" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var nombrePart = parts[0].Trim().Split('(');
                        var nombre = nombrePart[0].Trim();
                        var posicion = nombrePart.Length > 1 ? nombrePart[1].Trim(')', ' ') : "Desconocida";

                        goleadores.Add(new Goleador
                        {
                            NombreJugador = nombre,
                            Posicion = posicion,
                            Goles = 1, // Cada línea es un gol
                            PartidoId = partidoId,
                            LigaId = ligaId
                        });
                    }
                }
            }
            // Procesar tarjetas amarillas
            foreach (Match match in regexAmarillas.Matches(observaciones))
            {
                var nombreJugador = match.Groups[2].Value.Trim();
                goleadores.Add(new Goleador
                {
                    NombreJugador = nombreJugador,
                    TarjetasAmarillas = 1,
                    TarjetasRojas = 0,
                    Goles = 0
                });
            }

            // Procesar tarjetas rojas
            foreach (Match match in regexRojas.Matches(observaciones))
            {
                var nombreJugador = match.Groups[2].Value.Trim();
                goleadores.Add(new Goleador
                {
                    NombreJugador = nombreJugador,
                    TarjetasAmarillas = 0,
                    TarjetasRojas = 1,
                    Goles = 0
                });
            }
            return goleadores;
        }

        public async Task<IActionResult> Goleadores(int? ligaId)
        {
            var ligas = await _context.Ligas.ToListAsync();
            ViewBag.Ligas = new SelectList(ligas, "LigaId", "Nombre", ligaId);

            var viewModel = new GoleadoresLigaViewModel();

            if (ligaId.HasValue)
            {
                var liga = await _context.Ligas.FindAsync(ligaId.Value);
                viewModel.LigaId = ligaId.Value;
                viewModel.NombreLiga = liga?.Nombre;

                // Obtener goleadores de la base de datos
                var goleadoresDb = await _context.Goles
            .Include(g => g.Jugador)
            .Include(g => g.Equipo)
            .Include(g => g.Partido)
            .Where(g => g.Partido.LigaId == ligaId.Value && !g.EsAutogol)
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
                PartidosJugados = _context.EstadisticasJugador
    .Count(e => e.JugadorId == g.Key.JugadorId && 
          e.Partido.LigaId == ligaId.Value &&
          e.Partido.Estado == "Finalizado"),
                TarjetasAmarillas = _context.EstadisticasJugador
                    .Count(e => e.JugadorId == g.Key.JugadorId &&
                          e.Partido.LigaId == ligaId.Value &&
                          e.TarjetaAmarilla),
                TarjetasRojas = _context.EstadisticasJugador
                    .Count(e => e.JugadorId == g.Key.JugadorId &&
                          e.Partido.LigaId == ligaId.Value &&
                          e.TarjetaRoja)
            })
            .ToListAsync();

                // Obtener goleadores de observaciones no registrados en DB
                var partidosConObservaciones = await _context.Partidos
           .Include(p => p.EquipoLocal)
           .Include(p => p.EquipoVisitante)
           .Where(p => p.LigaId == ligaId.Value &&
                  p.Estado == "Finalizado" &&
                  !string.IsNullOrEmpty(p.Observaciones))
           .ToListAsync();

                var goleadoresObservaciones = partidosConObservaciones
                    .SelectMany(p => ExtraerGoleadoresDesdeObservaciones(p.Observaciones, p.PartidoId, p.LigaId))
                    .GroupBy(g => new { g.NombreJugador, g.EquipoNombre })
                    .Select(g => new Goleador
                    {
                        NombreJugador = g.Key.NombreJugador,
                        EquipoNombre = g.Key.EquipoNombre,
                        Posicion = g.First().Posicion,
                        Goles = g.Count(),
                        PartidosJugados = partidosConObservaciones
                            .Count(p => p.Observaciones.Contains(g.Key.NombreJugador)),
                        PromedioGoles = g.Count() / (decimal)Math.Max(1,
                            partidosConObservaciones.Count(p => p.Observaciones.Contains(g.Key.NombreJugador))),
                        TarjetasAmarillas = g.Sum(x => x.TarjetasAmarillas),
                        TarjetasRojas = g.Sum(x => x.TarjetasRojas)
                    })
                    .ToList();
                // Combinar y agrupar
                var todosGoleadores = goleadoresDb
            .Concat(goleadoresObservaciones)
            .GroupBy(g => g.NombreJugador)
            .Select(g => new Goleador
            {
                NombreJugador = g.Key,
                Posicion = g.First().Posicion,
                EquipoNombre = g.First().EquipoNombre,
                EscudoEquipo = g.First().EscudoEquipo,
                Goles = g.Sum(x => x.Goles),
                PartidosJugados = g.Sum(x => x.PartidosJugados),
                TarjetasAmarillas = g.Sum(x => x.TarjetasAmarillas),
                TarjetasRojas = g.Sum(x => x.TarjetasRojas),
                PromedioGoles = g.Sum(x => x.Goles) / (decimal)Math.Max(1, g.Max(x => x.PartidosJugados ?? 0))
            })
            .OrderByDescending(g => g.Goles)
            .ThenByDescending(g => g.PromedioGoles)
            .Take(20)
            .ToList();

                viewModel.Goleadores = todosGoleadores;
                viewModel.IncluyeDatosObservaciones = goleadoresObservaciones.Any();
                viewModel.GolesDeObservaciones = goleadoresObservaciones.Sum(g => g.Goles);

            }

            return View(viewModel);
        }
    }
}