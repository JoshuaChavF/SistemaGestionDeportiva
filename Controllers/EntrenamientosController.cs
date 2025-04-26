using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionDeportiva.Data;
using SistemaGestionDeportiva.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionDeportiva.Controllers
{
    [Authorize(Roles = "Administrador,Entrenador")]
    public class EntrenamientosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<EntrenamientosController> _logger;


        public EntrenamientosController(ApplicationDbContext context, UserManager<Usuario> userManager, ILogger<EntrenamientosController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            
        }

        // GET: Entrenamientos
        public async Task<IActionResult> Index(int? ligaId)
        {
            // 1. Obtener todas las ligas disponibles
            var ligasDisponibles = await _context.Ligas.ToListAsync();
            var model = new EntrenamientosViewModel();

            if (ligaId.HasValue)
            {
                model.EquiposDeLiga = _context.Equipos
                    .Where(e => e.LigaId == ligaId)
                    .ToList();
            }

            // Debug: Verificar ligas obtenidas
            _logger.LogInformation($"Ligas obtenidas: {ligasDisponibles.Count}");
            foreach (var liga in ligasDisponibles)
            {
                _logger.LogInformation($"Liga ID: {liga.LigaId}, Nombre: {liga.Nombre}");
            }

            // 2. Si no hay ligas, mostrar mensaje
            if (!ligasDisponibles.Any())
            {
                ViewBag.Mensaje = "No hay ligas disponibles";
                return View(new EntrenamientosViewModel { TodasLigas = ligasDisponibles });
            }

            // 3. Seleccionar liga (la primera si no se especifica)
            ligaId ??= ligasDisponibles.First().LigaId;

            // 4. Obtener la liga seleccionada con sus relaciones
            var ligaSeleccionada = await _context.Ligas
                .Include(l => l.Equipos)
                .Include(l => l.Entrenamientos)
                .FirstOrDefaultAsync(l => l.LigaId == ligaId);

            // Debug: Verificar liga seleccionada
            _logger.LogInformation($"Liga seleccionada: {(ligaSeleccionada != null ? ligaSeleccionada.Nombre : "NULL")}");

            // 5. Si no se encontró la liga (aunque debería existir)
            if (ligaSeleccionada == null)
            {
                _logger.LogError($"Liga ID {ligaId} no encontrada a pesar de existir en la lista");
                return NotFound();
            }

            // 6. Crear el ViewModel
            var viewModel = new EntrenamientosViewModel
            {
                LigaSeleccionada = ligaSeleccionada,
                LigaSeleccionadaId = ligaSeleccionada.LigaId,
                TodasLigas = ligasDisponibles,
                NumPartidos = await _context.Partidos.CountAsync(p => p.LigaId == ligaId),
                NumEntrenamientos = await _context.Entrenamientos.CountAsync(e => e.LigaId == ligaId)
            };

            return View(model);
        }
        public async Task<IActionResult> ListarLigas()
        {
            var ligas = await _context.Ligas.ToListAsync();
            return View(ligas);
        }
        [HttpGet]
        public IActionResult GetEquiposPorLiga(int ligaId)
        {
            try
            {
                // Verificar que la liga existe
                var ligaExiste = _context.Ligas.Any(l => l.LigaId == ligaId);
                if (!ligaExiste)
                {
                    return Json(new { success = false, message = "Liga no encontrada" });
                }

                // Obtener equipos ordenados por nombre
                var equipos = _context.Equipos
                    .Where(e => e.LigaId == ligaId)
                    .OrderBy(e => e.Nombre)
                    .Select(e => new {
                        id = e.EquipoId,
                        nombre = e.Nombre
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    equipos = equipos,
                    count = equipos.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }

        // GET: Entrenamientos/Calendario
        [HttpGet]
        public async Task<IActionResult> Calendario(int? ligaId = null) // Hacer el parámetro opcional
        {
            var viewModel = new CalendarioViewModel();
            var model = new EntrenamientosViewModel
            {
                TodasLigas = _context.Ligas.ToList(),
                LigaSeleccionadaId = ligaId
            };

            if (ligaId.HasValue)
            {
                model.EquiposDeLiga = _context.Equipos
                    .Where(e => e.LigaId == ligaId)
                    .ToList();

                model.NumPartidos = _context.Partidos
                    .Count(p => p.LigaId == ligaId);

                model.NumEntrenamientos = _context.Entrenamientos
                    .Count(e => e.LigaId == ligaId);
            }

            return View(model);
        }

        // GET: Entrenamientos/GetEntrenamientosCalendar
        public async Task<IActionResult> GetEntrenamientosCalendar()
        {
            var entrenamientos = await _context.Entrenamientos
                .Include(e => e.Equipo)
                .ToListAsync();

            var eventos = entrenamientos.Select(e => new
            {
                title = $"{e.Equipo.Nombre} - {e.Objetivo}",
                start = e.FechaHora.ToString("yyyy-MM-ddTHH:mm:ss"),
                url = Url.Action("Details", "Entrenamientos", new { id = e.EntrenamientoId }),
                color = e.FechaHora < DateTime.Now ? "#6c757d" : "#17a2b8"
            });

            return Json(eventos);
        }

        [HttpGet]
        public async Task<IActionResult> GetEntrenamientosPorLiga(int ligaId)
        {
            try
            {
                var entrenamientos = await _context.Entrenamientos
                    .Include(e => e.Equipo)
                    .Where(e => e.Equipo.LigaId == ligaId)
                    .Select(e => new
                    {
                        id = e.EntrenamientoId,
                        fechaHora = e.FechaHora,
                        objetivo = e.Objetivo,
                        equipoId = e.EquipoId,
                        equipoNombre = e.Equipo.Nombre
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    count = entrenamientos.Count,
                    entrenamientos
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // GET: Calendario integrado (Partidos + Entrenamientos)
        public async Task<IActionResult> CalendarioIntegrado(int ligaId)
        {
            var liga = await _context.Ligas
                .Include(l => l.Partidos)
                    .ThenInclude(p => p.EquipoLocal)
                .Include(l => l.Partidos)
                    .ThenInclude(p => p.EquipoVisitante)
                .Include(l => l.Entrenamientos)
                    .ThenInclude(e => e.Equipo)
                .FirstOrDefaultAsync(l => l.LigaId == ligaId);

            if (liga == null)
            {
                return NotFound();
            }

            var eventos = liga.Partidos.Select(p => new {
                id = p.PartidoId,
                title = $"⚽ {p.EquipoLocal?.Nombre} vs {p.EquipoVisitante?.Nombre}",
                start = p.FechaHora.ToString("o"),
                color = "#dc3545", // Rojo
                tipo = "partido",
                extendedProps = new { equipoId = 0 } // Añadido para coincidir con la estructura
            }).Concat(liga.Entrenamientos.Select(e => new {
                id = e.EntrenamientoId,
                title = $"🏋️ {e.Equipo?.Nombre} - {e.Objetivo}",
                start = e.FechaHora.ToString("o"),
                color = "#28a745", // Verde
                tipo = "entrenamiento",
                extendedProps = new { equipoId = e.EquipoId }
            }));

            return Json(eventos);
        }
        // POST: Programar nuevo entrenamiento
        [HttpPost]
        public async Task<IActionResult> ProgramarEntrenamiento([FromBody] EntrenamientoInputModel model)
        {
            // Validar que no choque con partidos
            var existePartido = await _context.Partidos
                .AnyAsync(p => p.LigaId == model.LigaId &&
                      p.FechaHora.Date == model.FechaHora.Date);

            if (existePartido)
                return BadRequest("Ya existe un partido programado para esa fecha");

            var entrenamiento = new Entrenamiento
            {
                LigaId = model.LigaId,
                FechaHora = model.FechaHora,
                Ubicacion = model.Ubicacion,
                Objetivo = model.Objetivo
            };

            _context.Add(entrenamiento);
            await _context.SaveChangesAsync();

            // Notificar a jugadores
            await NotificarEntrenamiento(entrenamiento);

            return Ok();
        }
        private async Task NotificarEntrenamiento(Entrenamiento entrenamiento)
        {
            var jugadores = await _context.Jugadores
                .Where(j => j.Equipo.LigaId == entrenamiento.LigaId)
                .Include(j => j.Usuario)
                .ToListAsync();

            foreach (var jugador in jugadores)
            {
                if (!string.IsNullOrEmpty(jugador.Usuario?.Email))
                {
                    
                }
            }
        }

        // GET: Entrenamientos/Create
        public async Task<IActionResult> Create()
        {
            ViewData["EquipoId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre");

            if (User.IsInRole("Entrenador"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                var entrenador = await _context.Entrenadores
                    .FirstOrDefaultAsync(e => e.UsuarioId == usuario.Id);

                if (entrenador != null)
                {
                    ViewData["EntrenadorId"] = entrenador.EntrenadorId;
                }
            }
            else
            {
                ViewData["EntrenadorId"] = new SelectList(await _context.Entrenadores.Include(e => e.Usuario).ToListAsync(), "EntrenadorId", "Usuario.NombreCompleto");
            }

            return View();
        }

        // POST: Entrenamientos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntrenamientoId,EquipoId,EntrenadorId,FechaHora,Ubicacion,Objetivo,Descripcion")] Entrenamiento entrenamiento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(entrenamiento);
                await _context.SaveChangesAsync();

                // Crear registros de asistencia para todos los jugadores del equipo
                var jugadores = await _context.Jugadores
                    .Where(j => j.EquipoId == entrenamiento.EquipoId)
                    .ToListAsync();

                foreach (var jugador in jugadores)
                {
                    var asistencia = new AsistenciaEntrenamiento
                    {
                        JugadorId = jugador.JugadorId,
                        EntrenamientoId = entrenamiento.EntrenamientoId,
                        Asistio = false
                    };

                    _context.Add(asistencia);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["EquipoId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", entrenamiento.EquipoId);

            if (User.IsInRole("Entrenador"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                var entrenador = await _context.Entrenadores
                    .FirstOrDefaultAsync(e => e.UsuarioId == usuario.Id);

                if (entrenador != null)
                {
                    ViewData["EntrenadorId"] = entrenador.EntrenadorId;
                }
            }
            else
            {
                ViewData["EntrenadorId"] = new SelectList(await _context.Entrenadores.Include(e => e.Usuario).ToListAsync(), "EntrenadorId", "Usuario.NombreCompleto", entrenamiento.EntrenadorId);
            }

            return View(entrenamiento);
        }
        [HttpGet]
        public async Task<IActionResult> ReporteAsistencias(int ligaId)
        {
            var reporte = await _context.Jugadores
                .Where(j => j.Equipo.LigaId == ligaId)
                .Select(j => new {
                    Jugador = j.Nombre,
                    Asistencias = j.Asistencias.Count(a => a.Asistio),
                    TotalEntrenamientos = _context.Entrenamientos.Count(e => e.LigaId == ligaId),
                    PorcentajeAsistencia = _context.Entrenamientos.Count(e => e.LigaId == ligaId) > 0 ?
                        j.Asistencias.Count(a => a.Asistio) * 100 / _context.Entrenamientos.Count(e => e.LigaId == ligaId) : 0
                })
                .OrderByDescending(r => r.PorcentajeAsistencia)
                .ToListAsync();

            return View(reporte);
        }

        [HttpGet]
        public async Task<IActionResult> EvolucionRendimiento(int jugadorId)
        {
            var metricas = await _context.MetricasEntrenamientos
                .Where(m => m.JugadorId == jugadorId)
                .OrderBy(m => m.Entrenamiento.FechaHora)
                .GroupBy(m => m.TipoMetrica)
                .Select(g => new {
                    Metrica = g.Key,
                    Datos = g.Select(m => new {
                        Fecha = m.Entrenamiento.FechaHora.ToString("dd/MM/yyyy"),
                        Valor = m.Valor
                    })
                })
                .ToListAsync();

            return Json(metricas);
        }

        // GET: Entrenamientos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrenamiento = await _context.Entrenamientos.FindAsync(id);
            if (entrenamiento == null)
            {
                return NotFound();
            }

            ViewData["EquipoId"] = new SelectList(await _context.Equipos.ToListAsync(), "EquipoId", "Nombre", entrenamiento.EquipoId);

            if (User.IsInRole("Entrenador"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                var entrenador = await _context.Entrenadores
                    .FirstOrDefaultAsync(e => e.UsuarioId == usuario.Id);

                if (entrenador != null)
                {
                    ViewData["EntrenadorId"] = entrenador.EntrenadorId;
                }
            }
            else
            {
                ViewData["EntrenadorId"] = new SelectList(await _context.Entrenadores.Include(e => e.Usuario).ToListAsync(), "EntrenadorId", "Usuario.NombreCompleto", entrenamiento.EntrenadorId);
            }

            return View(entrenamiento);
        }

        

        // GET: Entrenamientos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrenamiento = await _context.Entrenamientos
                .Include(e => e.Equipo)
                .Include(e => e.Entrenador)
                    .ThenInclude(e => e.Usuario)
                .FirstOrDefaultAsync(m => m.EntrenamientoId == id);

            if (entrenamiento == null)
            {
                return NotFound();
            }

            return View(entrenamiento);
        }

        // POST: Entrenamientos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entrenamiento = await _context.Entrenamientos.FindAsync(id);
            _context.Entrenamientos.Remove(entrenamiento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Entrenamientos/RegistrarAsistencia/5
        public async Task<IActionResult> RegistrarAsistencia(int id, Dictionary<int, bool> asistencias,
    Dictionary<int, string> notas)
        {

            var entrenamiento = await _context.Entrenamientos
                .Include(e => e.Equipo)
                .Include(e => e.Asistencias)
                    .ThenInclude(a => a.Jugador)
                        .ThenInclude(j => j.Usuario)
                .FirstOrDefaultAsync(e => e.EntrenamientoId == id);

            if (entrenamiento == null)
            {
                return NotFound();
            }

            return View(entrenamiento);
        }

        // POST: Entrenamientos/RegistrarAsistencia/5
        [HttpPost]
        public async Task<IActionResult> RegistrarAsistencia(int id, Dictionary<int, bool> asistencias)
        {
            var entrenamiento = await _context.Entrenamientos
                .Include(e => e.Asistencias)
                .FirstOrDefaultAsync(e => e.EntrenamientoId == id);

            if (entrenamiento == null)
            {
                return NotFound();
            }

            foreach (var asistencia in asistencias)
            {
                // Corrección: Comparar JugadorId en lugar de Asistio
                var registro = entrenamiento.Asistencias.FirstOrDefault(a => a.JugadorId == asistencia.Key);

                if (registro != null)
                {
                    registro.Asistio = asistencia.Value;
                }
                else
                {
                    // Si no existe registro previo, crear uno nuevo
                    entrenamiento.Asistencias.Add(new AsistenciaEntrenamiento
                    {
                        JugadorId = asistencia.Key,
                        Asistio = asistencia.Value,
                        EntrenamientoId = id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var entrenamiento = await _context.Entrenamientos
                .Include(e => e.Liga)
                .Include(e => e.Asistencias)
                    .ThenInclude(a => a.Jugador)
                .Include(e => e.Metricas)
                    .ThenInclude(m => m.Jugador)
                .FirstOrDefaultAsync(e => e.EntrenamientoId == id);

            if (entrenamiento == null)
            {
                return NotFound();
            }

            // Obtener todos los jugadores del equipo/liga para comparar
            var jugadores = await _context.Jugadores
                .Where(j => j.Equipo.LigaId == entrenamiento.LigaId)
                .ToListAsync();

            ViewBag.Jugadores = jugadores;
            return View(entrenamiento);
        }
        [HttpGet]
        public IActionResult GetEquiposCount(int ligaId)
        {
            var count = _context.Equipos.Count(e => e.LigaId == ligaId);
            return Ok(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarEntrenamientosBasicos([FromBody] GenerarEntrenamientosModel model)
        {
            try
            {
                // 1. Validar modelo
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos" });

                // 2. Obtener la liga con sus equipos
                var liga = await _context.Ligas
                    .Include(l => l.Equipos)
                    .FirstOrDefaultAsync(l => l.LigaId == model.LigaId);

                if (liga == null)
                    return NotFound(new { success = false, message = "Liga no encontrada" });

                if (liga.Equipos.Count == 0)
                    return BadRequest(new { success = false, message = "La liga no tiene equipos" });

                // 3. Configuración básica de entrenamientos
                var fechaInicio = DateTime.Today;
                var fechaFin = DateTime.Today.AddMonths(1); // Próximo mes
                var horaEntrenamiento = new TimeSpan(18, 0, 0); // 6:00 PM
                var duracion = 90; // 1.5 horas

                // 4. Generar entrenamientos (2 por semana por equipo)
                var entrenamientosGenerados = 0;

                foreach (var equipo in liga.Equipos)
                {
                    var fechaActual = fechaInicio;

                    while (fechaActual <= fechaFin)
                    {
                        // Martes y Jueves
                        var fechas = new[] {
                    GetNextWeekday(fechaActual, DayOfWeek.Tuesday),
                    GetNextWeekday(fechaActual, DayOfWeek.Thursday)
                };

                        foreach (var fecha in fechas)
                        {
                            var entrenamiento = new Entrenamiento
                            {
                                EquipoId = equipo.EquipoId,
                                LigaId = liga.LigaId,
                                FechaHora = fecha.Date.Add(horaEntrenamiento),
                                Descripcion = "Entrenamiento automático"
                            };

                            _context.Entrenamientos.Add(entrenamiento);
                            entrenamientosGenerados++;
                        }

                        fechaActual = fechaActual.AddDays(7); // Siguiente semana
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Se generaron {entrenamientosGenerados} entrenamientos",
                    total = entrenamientosGenerados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error interno: {ex.Message}"
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarEntrenamientosAutomaticos([FromForm] GenerarEntrenamientosModel model)
        {
            try
            {
                // Validación básica
                if (model.LigaId <= 0)
                {
                    return BadRequest(new { success = false, message = "ID de liga no válido" });
                }

                // Obtener la liga con sus equipos
                var liga = await _context.Ligas
                    .Include(l => l.Equipos)
                    .FirstOrDefaultAsync(l => l.LigaId == model.LigaId);

                if (liga == null)
                {
                    return NotFound(new { success = false, message = "Liga no encontrada" });
                }

                if (liga.Equipos.Count == 0)
                {
                    return BadRequest(new { success = false, message = "La liga no tiene equipos" });
                }

                // Convertir fechas
                var fechaInicio = DateTime.Parse(model.FechaInicio);
                var fechaFin = DateTime.Parse(model.FechaFin);
                var horaEntrenamiento = TimeSpan.Parse(model.HoraInicio);

                // Generar entrenamientos
                var entrenamientosGenerados = 0;
                var fechaActual = fechaInicio;

                while (fechaActual <= fechaFin)
                {
                    foreach (var equipo in liga.Equipos)
                    {
                        var entrenamiento = new Entrenamiento
                        {
                            EquipoId = equipo.EquipoId,
                            LigaId = liga.LigaId,
                            FechaHora = fechaActual.Date.Add(horaEntrenamiento),
                            Descripcion = "Entrenamiento automático generado"
                        };

                        _context.Entrenamientos.Add(entrenamiento);
                        entrenamientosGenerados++;
                    }

                    // Avanzar según la frecuencia
                    fechaActual = fechaActual.AddDays(model.Frecuencia);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Entrenamientos generados correctamente",
                    totalEntrenamientos = entrenamientosGenerados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, Json(new
                {
                    success = false,
                    message = $"Error al generar entrenamientos: {ex.Message}"
                }));
            }
        }

        public class GenerarEntrenamientosModel
        {
            public int LigaId { get; set; }
            public string FechaInicio { get; set; }
            public string FechaFin { get; set; }
            public int Frecuencia { get; set; }
            public string HoraInicio { get; set; }
        }
        // Método auxiliar
        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        public class GenerarEntrenamientosBasicosModel
        {
            public int LigaId { get; set; }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEntrenamiento([FromForm] NuevoEntrenamientoModel model)
        {
            // Validación manual adicional
            if (model.LigaId <= 0 || model.EquipoId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "IDs de equipo o liga no válidos",
                    details = $"LigaId: {model.LigaId}, EquipoId: {model.EquipoId}"
                });
            }

            try
            {
                // Verificar que el equipo pertenece a la liga
                var equipo = await _context.Equipos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EquipoId == model.EquipoId && e.LigaId == model.LigaId);

                if (equipo == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El equipo no pertenece a la liga especificada",
                        ligaId = model.LigaId,
                        equipoId = model.EquipoId
                    });
                }

                // Crear el entrenamiento
                var entrenamiento = new Entrenamiento
                {
                    EquipoId = model.EquipoId,
                    LigaId = model.LigaId,
                    FechaHora = model.FechaHora,
                    Descripcion = "Entrenamiento programado",
                    Objetivo = model.Objetivo,
                    Observaciones = "Practicar Tiros y marcaciones para el partido.",
                    Ubicacion = "Estadio Principal"

                };

                _context.Entrenamientos.Add(entrenamiento);
                await _context.SaveChangesAsync();

                return RedirectToAction("Calendario", "Entrenamientos");
            }
            catch (Exception ex)
            {
                
                    var fullMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Error interno", details = fullMessage });

            }
        }

        public class NuevoEntrenamientoModel
        {
            public int LigaId { get; set; }
            public int EquipoId { get; set; }
            public DateTime FechaHora { get; set; }
            public int Duracion { get; set; }
            public string Objetivo { get; set; }
        }
      

    }
}